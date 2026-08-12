#requires -Version 7.4

[CmdletBinding()]
param(
    [switch]$AsJson,
    [switch]$Fetch
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $AsJson) {
    throw 'Este checker requer -AsJson.'
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot

function Invoke-ExternalProcess {
    param(
        [Parameter(Mandatory)] [string]$FileName,
        [Parameter(Mandatory)] [string[]]$Arguments,
        [Parameter(Mandatory)] [string]$WorkingDirectory
    )

    $info = [System.Diagnostics.ProcessStartInfo]::new()
    $info.FileName = $FileName
    $info.WorkingDirectory = $WorkingDirectory
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $info.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    $info.StandardErrorEncoding = [System.Text.Encoding]::UTF8
    foreach ($argument in $Arguments) { [void]$info.ArgumentList.Add($argument) }
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $info
    [void]$process.Start()
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    [System.Threading.Tasks.Task]::WaitAll(@($stdoutTask, $stderrTask))
    return [pscustomobject]@{
        ExitCode = $process.ExitCode
        StdOut = $stdoutTask.Result
        StdErr = $stderrTask.Result
    }
}

function ConvertTo-SanitizedText {
    param([AllowNull()] [string]$Text)
    if ($null -eq $Text) { return '' }
    $value = $Text -replace '(?im)(password|pwd|token|api[_-]?key|authorization)\s*[:=]\s*[^\s]+', '$1=<redacted>'
    $value = $value -replace '(?i)(https?://)[^\s/@:]+:[^\s/@]+@', '$1<redacted>@'
    return ($value -replace '(?i)bearer\s+[^\s]+', 'Bearer <redacted>').Trim()
}

function New-Check {
    param(
        [string]$Name,
        [ValidateSet('passed', 'failed', 'environmentBlocked', 'skipped')] [string]$Status,
        [string]$Summary,
        [AllowNull()] [object]$Evidence
    )
    return [ordered]@{ name = $Name; status = $Status; summary = $Summary; evidence = $Evidence }
}

function Get-GitStatusSnapshot {
    param([string]$WorkingDirectory)
    $result = Invoke-ExternalProcess -FileName 'git' -Arguments @('status', '--porcelain=v1', '-z', '--untracked-files=normal') -WorkingDirectory $WorkingDirectory
    if ($result.ExitCode -ne 0) { throw "git status falhou: $(ConvertTo-SanitizedText ($result.StdErr + $result.StdOut))" }
    $parts = @($result.StdOut -split [string][char]0)
    $entries = [System.Collections.Generic.List[string]]::new()
    for ($index = 0; $index -lt $parts.Count; $index++) {
        $part = $parts[$index]
        if ([string]::IsNullOrEmpty($part)) { continue }
        $xy = if ($part.Length -ge 2) { $part.Substring(0, 2) } else { '??' }
        $path = if ($part.Length -ge 4) { $part.Substring(3) } else { $part }
        $record = "$xy`t$path"
        if ($xy[0] -in @('R', 'C') -or $xy[1] -in @('R', 'C')) {
            $index++
            if ($index -lt $parts.Count) { $record = "$record`t$($parts[$index])" }
        }
        $entries.Add($record)
    }
    return @($entries | Sort-Object -Unique)
}

function Get-FailureKind {
    param([string]$Output, [ValidateSet('restore', 'build')] [string]$Phase)
    $text = ConvertTo-SanitizedText $Output
    if ($Phase -eq 'restore' -and $text -match '(?i)(lock file.*(inconsistent|out of date)|NU1004|--locked-mode)') { return 'lockFileInconsistent' }
    if ($text -match '(?i)(NU1301|unable to load the service index|no such host|name or service not known|network.*(unavailable|error)|timed out|proxy|connection.*(refused|reset))') { return 'networkOrFeedUnavailable' }
    if ($text -match '(?i)(NETSDK[0-9]+|SDK.*(not found|could not be found)|A compatible installed .NET SDK)') { return 'sdkUnavailable' }
    if ($Phase -eq 'build') { return 'compilationOrBuildFailure' }
    return 'restoreFailure'
}

function Add-DiagnosticWarnings {
    param([string]$Output)
    foreach ($line in @($Output -split "`r?`n")) {
        $trimmedLine = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmedLine)) { continue }
        if ($trimmedLine -match '(?i)^(?<count>\d+)\s+(warning|aviso)') {
            if ([int]$Matches['count'] -eq 0) { continue }
        }
        if ($trimmedLine -match '(?i)\b(warning|aviso)\b') {
            $warnings.Add($trimmedLine)
        }
    }
}

function Get-ManualRequirements {
    param([string]$WorkingDirectory, [string]$CurrentFront)
    $patch = Invoke-ExternalProcess -FileName 'git' -Arguments @('diff', '--no-ext-diff', 'origin/main..HEAD') -WorkingDirectory $WorkingDirectory
    $worktreePatch = Invoke-ExternalProcess -FileName 'git' -Arguments @('diff', '--no-ext-diff') -WorkingDirectory $WorkingDirectory
    $paths = Invoke-ExternalProcess -FileName 'git' -Arguments @('diff', '--name-only', '-z', 'origin/main..HEAD') -WorkingDirectory $WorkingDirectory
    $worktreePaths = Invoke-ExternalProcess -FileName 'git' -Arguments @('diff', '--name-only', '-z') -WorkingDirectory $WorkingDirectory
    if ($patch.ExitCode -ne 0 -or $worktreePatch.ExitCode -ne 0 -or $paths.ExitCode -ne 0 -or $worktreePaths.ExitCode -ne 0) { return @() }
    $frontPattern = 'B000|B001|B002|B003|B004|B005|B006'
    if (-not [string]::IsNullOrWhiteSpace($CurrentFront)) { $frontPattern = "$frontPattern|$([regex]::Escape($CurrentFront))" }
    if (($patch.StdOut + $worktreePatch.StdOut) -notmatch "(?i)\b($frontPattern)\b") { return @() }
    $allPaths = @(
        (@($paths.StdOut -split [string][char]0) + @($worktreePaths.StdOut -split [string][char]0)) |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            Sort-Object -Unique
    )
    $requirements = [System.Collections.Generic.List[object]]::new()
    foreach ($path in $allPaths) {
        $isRelevant = $path -match '^(Src|Tests|Tools|scripts)/' -or $path -match '^Docs/Implementation/B00[0-6].*\.md$' -or $path -in @('CHANGELOG.md', 'Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md', 'Docs/Foundation/24-PLANO_IMPLEMENTACAO_REAL_POR_SPRINTS.md', 'AGENTS.md') -or $path -match '^Docs/Foundation/'
        if ($isRelevant) {
            $requirements.Add([ordered]@{
                path = $path
                reason = 'O patch menciona um spike B000–B006 ou a frente vigente.'
                requiredHumanCheck = 'Confirmar os gates de encerramento e a coerência documental aplicáveis ao escopo alterado.'
            })
        }
    }
    return @($requirements)
}

$checks = [System.Collections.Generic.List[object]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()
$commands = [System.Collections.Generic.List[object]]::new()
$environmentBlocked = $false
$failed = $false
$workingTreeDirty = $false
$remoteFetchStatus = if ($Fetch) { 'notRun' } else { 'notRequested' }
$remoteReadiness = if ($Fetch) { 'pending' } else { 'unverified' }
$pushReadiness = 'readyLocal'
$gitContext = [ordered]@{ branch = $null; ahead = $null; behind = $null; baseRef = 'origin/main'; workingTreeDirty = $false; preexistingChanges = @(); newNonIgnoredChanges = @() }

try {
    $gitContext.preexistingChanges = @(Get-GitStatusSnapshot -WorkingDirectory $repositoryRoot)
    $workingTreeDirty = $gitContext.preexistingChanges.Count -gt 0
    $gitContext.workingTreeDirty = $workingTreeDirty
    if ($workingTreeDirty) {
        $warnings.Add('A working tree já possuía alterações não ignoradas antes da execução; push requer revisão humana.')
        $checks.Add((New-Check 'git.statusPre' 'skipped' 'A working tree já estava suja antes da execução.' $gitContext.preexistingChanges))
    }
    else {
        $checks.Add((New-Check 'git.statusPre' 'passed' 'A working tree estava limpa antes da execução.' $null))
    }
    if ($Fetch) {
        $fetchResult = Invoke-ExternalProcess -FileName 'git' -Arguments @('fetch', 'origin') -WorkingDirectory $repositoryRoot
        $commands.Add([ordered]@{ command = 'git fetch origin'; exitCode = $fetchResult.ExitCode; output = ConvertTo-SanitizedText ($fetchResult.StdOut + $fetchResult.StdErr) })
        if ($fetchResult.ExitCode -eq 0) { $remoteFetchStatus = 'succeeded'; $remoteReadiness = 'confirmed'; $checks.Add((New-Check 'git.fetch' 'passed' 'origin atualizado.' $null)) }
        else { $remoteFetchStatus = 'failed'; $remoteReadiness = 'unverified'; $environmentBlocked = $true; $checks.Add((New-Check 'git.fetch' 'environmentBlocked' 'Não foi possível atualizar origin.' (ConvertTo-SanitizedText ($fetchResult.StdOut + $fetchResult.StdErr)))) }
    }

    $branch = Invoke-ExternalProcess -FileName 'git' -Arguments @('branch', '--show-current') -WorkingDirectory $repositoryRoot
    $gitContext.branch = $branch.StdOut.Trim()
    if ($branch.ExitCode -ne 0) { $environmentBlocked = $true; $checks.Add((New-Check 'git.branch' 'environmentBlocked' 'Não foi possível identificar a branch atual.' (ConvertTo-SanitizedText $branch.StdErr))) }
    elseif ($gitContext.branch -ne 'main') { $failed = $true; $checks.Add((New-Check 'git.branch' 'failed' "A branch atual deve ser main; encontrada '$($gitContext.branch)'." $null)) }
    else { $checks.Add((New-Check 'git.branch' 'passed' 'A branch atual é main.' $null)) }

    $revision = Invoke-ExternalProcess -FileName 'git' -Arguments @('rev-list', '--left-right', '--count', 'origin/main...HEAD') -WorkingDirectory $repositoryRoot
    if ($revision.ExitCode -ne 0) { $environmentBlocked = $true; $checks.Add((New-Check 'git.remoteBase' 'environmentBlocked' 'origin/main não está disponível para comparação.' (ConvertTo-SanitizedText ($revision.StdOut + $revision.StdErr)))) }
    else {
        $counts = @($revision.StdOut.Trim() -split '\s+')
        $gitContext.behind = [int]$counts[0]
        $gitContext.ahead = [int]$counts[1]
        if ($gitContext.behind -gt 0) { $failed = $true; $checks.Add((New-Check 'git.remoteBase' 'failed' "A main local está $($gitContext.behind) commit(s) atrás de origin/main." $gitContext)) }
        else { $checks.Add((New-Check 'git.remoteBase' 'passed' "Comparação concluída: $($gitContext.ahead) à frente e $($gitContext.behind) atrás." $gitContext)) }
    }

    foreach ($diffSpec in @(
        [ordered]@{ Name = 'git.diffInterval'; Arguments = @('diff', '--check', 'origin/main..HEAD'); Summary = 'O diff contra origin/main não contém erro de whitespace.' },
        [ordered]@{ Name = 'git.diffWorkingTree'; Arguments = @('diff', '--check'); Summary = 'O diff da working tree não contém erro de whitespace.' }
    )) {
        $diff = Invoke-ExternalProcess -FileName 'git' -Arguments $diffSpec.Arguments -WorkingDirectory $repositoryRoot
        $output = ConvertTo-SanitizedText ($diff.StdOut + $diff.StdErr)
        $commands.Add([ordered]@{ command = "git $($diffSpec.Arguments -join ' ')"; exitCode = $diff.ExitCode; output = $output })
        if ($diff.ExitCode -eq 0) { $checks.Add((New-Check $diffSpec.Name 'passed' $diffSpec.Summary $null)) }
        else { $failed = $true; $checks.Add((New-Check $diffSpec.Name 'failed' $diffSpec.Summary.Replace('não contém', 'contém') $output)) }
    }

    $listed = Invoke-ExternalProcess -FileName 'git' -Arguments @('ls-files', '-z', '--', 'Tools', 'scripts') -WorkingDirectory $repositoryRoot
    $parseTargets = [System.Collections.Generic.List[string]]::new()
    if ($listed.ExitCode -eq 0) {
        foreach ($relativePath in @($listed.StdOut -split [string][char]0)) { if ($relativePath -match '\.ps1$') { $parseTargets.Add((Join-Path $repositoryRoot $relativePath)) } }
    }
    else { $environmentBlocked = $true; $checks.Add((New-Check 'powershell.parse' 'environmentBlocked' 'Não foi possível obter os scripts versionados.' (ConvertTo-SanitizedText $listed.StdErr))) }
    if (-not $parseTargets.Contains($PSCommandPath)) { $parseTargets.Add($PSCommandPath) }
    if (-not $environmentBlocked) {
        $parseErrors = [System.Collections.Generic.List[string]]::new()
        foreach ($target in $parseTargets) {
            $errors = $null
            [void][System.Management.Automation.Language.Parser]::ParseFile($target, [ref]$null, [ref]$errors)
            foreach ($error in @($errors)) { $parseErrors.Add("${target}:$($error.Extent.StartLineNumber): $($error.Message)") }
        }
        if ($parseErrors.Count -eq 0) { $checks.Add((New-Check 'powershell.parse' 'passed' "Parse concluído para $($parseTargets.Count) script(s)." @($parseTargets))) }
        else { $failed = $true; $checks.Add((New-Check 'powershell.parse' 'failed' 'Há erro de parse em scripts PowerShell.' @($parseErrors))) }
    }

    foreach ($unitTest in @(
        [ordered]@{ Name = 'tests.serviceSourceContract'; RelativePath = 'Tests\ServiceSourceContract\Test-ApiPlanServiceSourceContract.ps1'; Command = 'pwsh -NoProfile -File Tests/ServiceSourceContract/Test-ApiPlanServiceSourceContract.ps1'; Passed = 'Teste unitário do parser Service Source concluído.'; Failed = 'Teste unitário do parser Service Source falhou.'; Missing = 'Teste unitário do parser Service Source não encontrado.' },
        [ordered]@{ Name = 'tests.metadataIntegrity'; RelativePath = 'Tests\MetadataIntegrity\Test-ApiPlanMetadataIntegrity.ps1'; Command = 'pwsh -NoProfile -File Tests/MetadataIntegrity/Test-ApiPlanMetadataIntegrity.ps1'; Passed = 'Teste unitário da integridade B067 concluído.'; Failed = 'Teste unitário da integridade B067 falhou.'; Missing = 'Teste unitário da integridade B067 não encontrado.' },
        [ordered]@{ Name = 'tests.apiObjectOwnership'; RelativePath = 'Tests\ApiObjectOwnership\Test-ApiPlanApiObjectOwnership.ps1'; Command = 'pwsh -NoProfile -File Tests/ApiObjectOwnership/Test-ApiPlanApiObjectOwnership.ps1'; Passed = 'Teste unitário da posse B087 do API Object concluído.'; Failed = 'Teste unitário da posse B087 do API Object falhou.'; Missing = 'Teste unitário da posse B087 do API Object não encontrado.' },
        [ordered]@{ Name = 'tests.ownedObjectDescription'; RelativePath = 'Tests\OwnershipDescriptions\Test-ApiPlanOwnedObjectDescription.ps1'; Command = 'pwsh -NoProfile -File Tests/OwnershipDescriptions/Test-ApiPlanOwnedObjectDescription.ps1'; Passed = 'Teste unitário das descrições canônicas e legadas de ownership concluído.'; Failed = 'Teste unitário das descrições canônicas e legadas de ownership falhou.'; Missing = 'Teste unitário das descrições canônicas e legadas de ownership não encontrado.' },
        [ordered]@{ Name = 'tests.generatedApiRemovalPlan'; RelativePath = 'Tests\GeneratedApiRemoval\Test-ApiPlanGeneratedApiRemovalPlan.ps1'; Command = 'pwsh -NoProfile -File Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPlan.ps1'; Passed = 'Teste unitário do plano de remoção B086 concluído.'; Failed = 'Teste unitário do plano de remoção B086 falhou.'; Missing = 'Teste unitário do plano de remoção B086 não encontrado.' },
        [ordered]@{ Name = 'tests.generatedApiRemovalPreflight'; RelativePath = 'Tests\GeneratedApiRemoval\Test-ApiPlanGeneratedApiRemovalPreflight.ps1'; Command = 'pwsh -NoProfile -File Tests/GeneratedApiRemoval/Test-ApiPlanGeneratedApiRemovalPreflight.ps1'; Passed = 'Teste unitário do preflight B086 antes do primeiro Delete concluído.'; Failed = 'Teste unitário do preflight B086 antes do primeiro Delete falhou.'; Missing = 'Teste unitário do preflight B086 antes do primeiro Delete não encontrado.' },
        [ordered]@{ Name = 'tests.transactionSyncComparer'; RelativePath = 'Tests\TransactionSync\Test-ApiPlanTransactionSyncComparer.ps1'; Command = 'pwsh -NoProfile -File Tests/TransactionSync/Test-ApiPlanTransactionSyncComparer.ps1'; Passed = 'Teste unitário do diff B085 de sincronização concluído.'; Failed = 'Teste unitário do diff B085 de sincronização falhou.'; Missing = 'Teste unitário do diff B085 de sincronização não encontrado.' },
        [ordered]@{ Name = 'tests.transactionSyncFieldSelection'; RelativePath = 'Tests\TransactionSync\Test-ApiPlanTransactionSyncFieldSelection.ps1'; Command = 'pwsh -NoProfile -File Tests/TransactionSync/Test-ApiPlanTransactionSyncFieldSelection.ps1'; Passed = 'Teste unitário da seleção ordenada de campos B085 concluído.'; Failed = 'Teste unitário da seleção ordenada de campos B085 falhou.'; Missing = 'Teste unitário da seleção ordenada de campos B085 não encontrado.' },
        [ordered]@{ Name = 'tests.applicationFinalReport'; RelativePath = 'Tests\ApplicationFinalReport\Test-ApiPlanApplicationFinalReport.ps1'; Command = 'pwsh -NoProfile -File Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport.ps1'; Passed = 'Teste unitário do relatório final B081 concluído.'; Failed = 'Teste unitário do relatório final B081 falhou.'; Missing = 'Teste unitário do relatório final B081 não encontrado.' },
        [ordered]@{ Name = 'tests.collisionUx'; RelativePath = 'Tests\CollisionUx\Test-ApiPlanCollisionConflict.ps1'; Command = 'pwsh -NoProfile -File Tests/CollisionUx/Test-ApiPlanCollisionConflict.ps1'; Passed = 'Teste unitário da UX residual B083 de conflitos concluído.'; Failed = 'Teste unitário da UX residual B083 de conflitos falhou.'; Missing = 'Teste unitário da UX residual B083 de conflitos não encontrado.' },
        [ordered]@{ Name = 'tests.transactionFolderReuse'; RelativePath = 'Tests\TransactionFolder\Test-ApiPlanTransactionFolderReusePolicy.ps1'; Command = 'pwsh -NoProfile -File Tests/TransactionFolder/Test-ApiPlanTransactionFolderReusePolicy.ps1'; Passed = 'Teste unitário do reuso de Folder com aviso concluído.'; Failed = 'Teste unitário do reuso de Folder com aviso falhou.'; Missing = 'Teste unitário do reuso de Folder com aviso não encontrado.' },
        [ordered]@{ Name = 'tests.wizardPreferences'; RelativePath = 'Tests\WizardPreferences\Test-PrototypeWizardPreferences.ps1'; Command = 'pwsh -NoProfile -File Tests/WizardPreferences/Test-PrototypeWizardPreferences.ps1'; Passed = 'Teste unitário das preferências do wizard concluído.'; Failed = 'Teste unitário das preferências do wizard falhou.'; Missing = 'Teste unitário das preferências do wizard não encontrado.' },
        [ordered]@{ Name = 'tests.wizardNavigation'; RelativePath = 'Tests\WizardNavigation\Test-PrototypeWizardBusinessComponentNavigationPolicy.ps1'; Command = 'pwsh -NoProfile -File Tests/WizardNavigation/Test-PrototypeWizardBusinessComponentNavigationPolicy.ps1'; Passed = 'Teste unitário da navegação do wizard concluído.'; Failed = 'Teste unitário da navegação do wizard falhou.'; Missing = 'Teste unitário da navegação do wizard não encontrado.' },
        [ordered]@{ Name = 'tests.wizardContractCreateRequired'; RelativePath = 'Tests\WizardContract\Test-PrototypeWizardCreateRequiredPrimaryKeyOptional.ps1'; Command = 'pwsh -NoProfile -File Tests/WizardContract/Test-PrototypeWizardCreateRequiredPrimaryKeyOptional.ps1'; Passed = 'Teste unitário do contrato de wizard CreateRequired concluído.'; Failed = 'Teste unitário do contrato de wizard CreateRequired falhou.'; Missing = 'Teste unitário do contrato de wizard CreateRequired não encontrado.' },
        [ordered]@{ Name = 'tests.wizardContractAutonumberCompositeKey'; RelativePath = 'Tests\WizardContract\Test-PrototypeWizardAutonumberCompositeKey.ps1'; Command = 'pwsh -NoProfile -File Tests/WizardContract/Test-PrototypeWizardAutonumberCompositeKey.ps1'; Passed = 'Teste unitário de autonumeração e chave composta concluído.'; Failed = 'Teste unitário de autonumeração e chave composta falhou.'; Missing = 'Teste unitário de autonumeração e chave composta não encontrado.' },
        [ordered]@{ Name = 'tests.wizardContractGenerationStateReader'; RelativePath = 'Tests\WizardContract\Test-ApiPlanGenerationStateReaderGetAllIndex.ps1'; Command = 'pwsh -NoProfile -File Tests/WizardContract/Test-ApiPlanGenerationStateReaderGetAllIndex.ps1'; Passed = 'Teste unitário do leitor de estado de geração concluído.'; Failed = 'Teste unitário do leitor de estado de geração falhou.'; Missing = 'Teste unitário do leitor de estado de geração não encontrado.' },
        [ordered]@{ Name = 'tests.writePreflightScope'; RelativePath = 'Tests\WritePreflight\Test-ApiPlanWritePreflightScope.ps1'; Command = 'pwsh -NoProfile -File Tests/WritePreflight/Test-ApiPlanWritePreflightScope.ps1'; Passed = 'Teste unitário do escopo de preflight de escrita concluído.'; Failed = 'Teste unitário do escopo de preflight de escrita falhou.'; Missing = 'Teste unitário do escopo de preflight de escrita não encontrado.' },
        [ordered]@{ Name = 'tests.businessComponentWriterVariableContract'; RelativePath = 'Tests\BusinessComponentWriter\Test-ApiPlanBusinessComponentWriterVariableContract.ps1'; Command = 'pwsh -NoProfile -File Tests/BusinessComponentWriter/Test-ApiPlanBusinessComponentWriterVariableContract.ps1'; Passed = 'Teste unitário do contrato de variáveis do writer Business Component concluído.'; Failed = 'Teste unitário do contrato de variáveis do writer Business Component falhou.'; Missing = 'Teste unitário do contrato de variáveis do writer Business Component não encontrado.' },
        [ordered]@{ Name = 'tests.listProcedureReencounterPolicy'; RelativePath = 'Tests\ListProcedure\Test-ApiPlanListProcedureReencounterPolicy.ps1'; Command = 'pwsh -NoProfile -File Tests/ListProcedure/Test-ApiPlanListProcedureReencounterPolicy.ps1'; Passed = 'Teste unitário do reencontro B070 de List concluído.'; Failed = 'Teste unitário do reencontro B070 de List falhou.'; Missing = 'Teste unitário do reencontro B070 de List não encontrado.' },
        [ordered]@{ Name = 'tests.requiredMemberSemantics'; RelativePath = 'Tests\RequiredSemantics\Test-RequiredMemberSemanticsConsistency.ps1'; Command = 'pwsh -NoProfile -File Tests/RequiredSemantics/Test-RequiredMemberSemanticsConsistency.ps1'; Passed = 'Teste unitário da coerência semântica de Required concluído.'; Failed = 'Teste unitário da coerência semântica de Required falhou.'; Missing = 'Teste unitário da coerência semântica de Required não encontrado.' },
        [ordered]@{ Name = 'tests.openApiContractMarks'; RelativePath = 'Tests\OpenApiContract\Test-ApiPlanOpenApiContractMarks.ps1'; Command = 'pwsh -NoProfile -File Tests/OpenApiContract/Test-ApiPlanOpenApiContractMarks.ps1'; Passed = 'Teste unitário das marcações do contrato OpenAPI concluído.'; Failed = 'Teste unitário das marcações do contrato OpenAPI falhou.'; Missing = 'Teste unitário das marcações do contrato OpenAPI não encontrado.' },
        [ordered]@{ Name = 'tests.openApiClientContractValidity'; RelativePath = 'Tests\OpenApiContract\Test-OpenApiClientContractValidity.ps1'; Command = 'pwsh -NoProfile -File Tests/OpenApiContract/Test-OpenApiClientContractValidity.ps1'; Passed = 'Teste unitário da validade do contrato de cliente OpenAPI concluído.'; Failed = 'Teste unitário da validade do contrato de cliente OpenAPI falhou.'; Missing = 'Teste unitário da validade do contrato de cliente OpenAPI não encontrado.' },
        [ordered]@{ Name = 'tests.issueForms'; RelativePath = 'Tests\IssueForms\Test-GitHubIssueFormsYaml.ps1'; Command = 'pwsh -NoProfile -File Tests/IssueForms/Test-GitHubIssueFormsYaml.ps1'; Passed = 'Teste unitário dos YAML / Issue Forms do GitHub concluído.'; Failed = 'Teste unitário dos YAML / Issue Forms do GitHub falhou.'; Missing = 'Teste unitário dos YAML / Issue Forms do GitHub não encontrado.'; EnvironmentBlocked = 'Ambiente sem python3/pyyaml para parse real dos YAML / Issue Forms.' }
    )) {
        $testPath = Join-Path $repositoryRoot $unitTest.RelativePath
        if (Test-Path -LiteralPath $testPath -PathType Leaf) {
            $testResult = Invoke-ExternalProcess -FileName 'pwsh' -Arguments @('-NoProfile', '-File', $testPath) -WorkingDirectory $repositoryRoot
            $testOutput = ConvertTo-SanitizedText ($testResult.StdOut + $testResult.StdErr)
            $commands.Add([ordered]@{ command = $unitTest.Command; exitCode = $testResult.ExitCode; output = $testOutput })
            if ($testResult.ExitCode -eq 0) { $checks.Add((New-Check $unitTest.Name 'passed' $unitTest.Passed $testOutput)) }
            elseif ($testResult.ExitCode -eq 2) {
                $environmentBlocked = $true
                $blockedSummary = if ($unitTest.Contains('EnvironmentBlocked')) { $unitTest.EnvironmentBlocked } else { "Ambiente incompleto para o check $($unitTest.Name)." }
                $checks.Add((New-Check $unitTest.Name 'environmentBlocked' $blockedSummary $testOutput))
            }
            else { $failed = $true; $checks.Add((New-Check $unitTest.Name 'failed' $unitTest.Failed $testOutput)) }
        }
        else {
            $failed = $true
            $checks.Add((New-Check $unitTest.Name 'failed' $unitTest.Missing $testPath))
        }
    }

    foreach ($dotnetSpec in @(
        [ordered]@{ Name = 'dotnet.restore'; Phase = 'restore'; Arguments = @('restore', 'Src\GenexusOpenApiBuilder.sln', '--locked-mode'); Command = 'dotnet restore Src\GenexusOpenApiBuilder.sln --locked-mode' },
        [ordered]@{ Name = 'dotnet.build'; Phase = 'build'; Arguments = @('build', 'Src\GenexusOpenApiBuilder.sln', '--configuration', 'Release', '--no-restore'); Command = 'dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release --no-restore' }
    )) {
        $commandResult = Invoke-ExternalProcess -FileName 'dotnet' -Arguments $dotnetSpec.Arguments -WorkingDirectory $repositoryRoot
        $output = ConvertTo-SanitizedText ($commandResult.StdOut + $commandResult.StdErr)
        Add-DiagnosticWarnings -Output $output
        $commands.Add([ordered]@{ command = $dotnetSpec.Command; exitCode = $commandResult.ExitCode; output = $output })
        if ($commandResult.ExitCode -eq 0) { $checks.Add((New-Check $dotnetSpec.Name 'passed' "$($dotnetSpec.Phase) concluído." $null)); continue }
        $kind = Get-FailureKind -Output $output -Phase $dotnetSpec.Phase
        $status = if ($kind -in @('networkOrFeedUnavailable', 'sdkUnavailable')) { 'environmentBlocked' } else { 'failed' }
        if ($status -eq 'environmentBlocked') { $environmentBlocked = $true } else { $failed = $true }
        $checks.Add((New-Check $dotnetSpec.Name $status "$($dotnetSpec.Phase) falhou: $kind." $output))
    }
}
catch {
    $environmentBlocked = $true
    $checks.Add((New-Check 'checker.execution' 'environmentBlocked' 'O checker não conseguiu concluir sua execução.' (ConvertTo-SanitizedText $_.Exception.Message)))
}
finally {
    try {
        $postStatus = Get-GitStatusSnapshot -WorkingDirectory $repositoryRoot
        $gitContext.newNonIgnoredChanges = @($postStatus | Where-Object { $_ -notin $gitContext.preexistingChanges })
        if ($gitContext.newNonIgnoredChanges.Count -gt 0) { $failed = $true; $checks.Add((New-Check 'git.statusPost' 'failed' 'O checker deixou mudanças novas não ignoradas.' $gitContext.newNonIgnoredChanges)) }
        else { $checks.Add((New-Check 'git.statusPost' 'passed' 'O checker não deixou mudanças novas não ignoradas.' $null)) }
    }
    catch { $environmentBlocked = $true; $checks.Add((New-Check 'git.statusPost' 'environmentBlocked' 'Não foi possível comparar o status Git final.' (ConvertTo-SanitizedText $_.Exception.Message))) }
}

$currentFront = $null
$checkpoint = Join-Path $repositoryRoot 'Docs\STATUS_ATUAL_E_PROXIMO_PASSO.md'
if (Test-Path -LiteralPath $checkpoint -PathType Leaf) {
    $front = [regex]::Match((Get-Content -LiteralPath $checkpoint -Raw), '(?s)## Próxima ação única.*?\b(B00[0-6])\b')
    if ($front.Success) { $currentFront = $front.Groups[1].Value }
}
$manualRequired = @(Get-ManualRequirements -WorkingDirectory $repositoryRoot -CurrentFront $currentFront)
$incompleteReasons = [System.Collections.Generic.List[string]]::new()
if ($workingTreeDirty) { $incompleteReasons.Add('workingTreeDirty') }
if ($manualRequired.Count -gt 0) { $incompleteReasons.Add('manualRequired') }
$incomplete = $incompleteReasons.Count -gt 0
if ($environmentBlocked) { $overallStatus = 'environmentBlocked'; $exitCode = 2; $pushReadiness = 'environmentBlocked' }
elseif ($failed) { $overallStatus = 'failed'; $exitCode = 1; $pushReadiness = 'blocked' }
elseif ($incomplete) { $overallStatus = 'incomplete'; $exitCode = 3; $pushReadiness = 'blocked' }
else { $overallStatus = 'passed'; $exitCode = 0; $pushReadiness = if ($Fetch) { 'readyRemote' } else { 'readyLocal' } }
$localReadiness = if ($overallStatus -eq 'passed') { 'ready' } else { 'blocked' }

[ordered]@{
    status = $overallStatus
    exitCode = $exitCode
    pushReadiness = $pushReadiness
    localReadiness = $localReadiness
    remoteReadiness = $remoteReadiness
    remoteFetchStatus = $remoteFetchStatus
    gitContext = $gitContext
    commands = @($commands)
    checks = @($checks)
    warnings = @($warnings)
    incompleteReasons = @($incompleteReasons)
    manualRequired = @($manualRequired)
    notCovered = @('Validação funcional na IDE GeneXus, acesso a KB, instalação de DLL e scripts em Tools não são executados.', 'manualRequired e workingTreeDirty exigem revisão humana; não comprovam fechamento semântico.')
} | ConvertTo-Json -Depth 8

exit $exitCode
