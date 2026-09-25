#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)

    if (-not $Condition) {
        throw "ASSERT_TRUE_FAILED: $Message"
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)

    if ($Expected -ne $Actual) {
        throw "ASSERT_EQUAL_FAILED: $Message (expected='$Expected' actual='$Actual')"
    }
}

$repositoryRoot = Join-Path $PSScriptRoot '..\..'
$sourceRoot = Join-Path $repositoryRoot 'Src\Extension'

# Inventário fechado de chamadas físicas que pertencem ao pipeline F2. A preferência
# persistida do Wizard usa um File próprio e fica fora deste contrato deliberadamente.
$expectedCalls = @(
    @{ File = 'Package.cs'; Invocation = 'transaction.Save'; FaultPoint = 'BusinessComponentEnablementSave'; Route = 'Persist' },
    @{ File = 'Diagnostics\ApiPlanTransactionFolder.cs'; Invocation = 'folder.Save'; FaultPoint = 'FolderSave'; Route = 'Persist' },
    @{ File = 'Diagnostics\ApiPlanSdtWriter.cs'; Invocation = 'folder.Save'; FaultPoint = 'FolderSave'; Route = 'Persist' },
    @{ File = 'Diagnostics\ApiPlanSdtWriter.cs'; Invocation = 'existingSdt.Save'; FaultPoint = 'SdtSave'; Route = 'Persist' },
    @{ File = 'Diagnostics\ApiPlanSdtWriter.cs'; Invocation = 'sdt.Save'; FaultPoint = 'SdtSave'; Route = 'Persist' },
    @{ File = 'Diagnostics\ApiPlanProcedureWriter.cs'; Invocation = 'existingProcedure.Save'; FaultPoint = 'ProcedureSave'; Route = 'Persist' },
    @{ File = 'Diagnostics\ApiPlanProcedureWriter.cs'; Invocation = 'procedure.Save'; FaultPoint = 'ProcedureSave'; Route = 'Persist' },
    @{ File = 'Diagnostics\ApiPlanApiObjectWriter.cs'; Invocation = 'api.Save'; FaultPoint = 'ApiSave'; Route = 'Persist' },
    @{ File = 'Diagnostics\ApiPlanBusinessComponentWriter.cs'; Invocation = 'procedure.Save'; FaultPoint = 'ProcedureSave'; Route = 'Executor' },
    @{ File = 'Diagnostics\ApiPlanBusinessComponentWriter.cs'; Invocation = 'api.Save'; FaultPoint = 'ApiSave'; Route = 'Executor' },
    @{ File = 'Diagnostics\ApiPlanListProcedureWriter.cs'; Invocation = 'procedure.Save'; FaultPoint = 'ProcedureSave'; Route = 'Executor' },
    @{ File = 'Diagnostics\ApiPlanListProcedureWriter.cs'; Invocation = 'api.Save'; FaultPoint = 'ApiSave'; Route = 'Executor' },
    @{ File = 'Diagnostics\ApiPlanMetadataFileWriter.cs'; Invocation = 'file.Save'; FaultPoint = 'MetadataSave'; Route = 'Persist' },
    @{ File = 'Diagnostics\ApiPlanOrphanMetadataRecovery.cs'; Invocation = 'file.Save'; FaultPoint = 'B115MetadataSave'; Route = 'Persist' },
    @{ File = 'Diagnostics\ApiPlanGeneratedApiRemover.cs'; Invocation = 'procedure.Delete'; FaultPoint = 'ProcedureDelete'; Route = 'Persist' },
    @{ File = 'Diagnostics\ApiPlanGeneratedApiRemover.cs'; Invocation = 'api.Delete'; FaultPoint = 'ApiDelete'; Route = 'Persist' },
    @{ File = 'Diagnostics\ApiPlanGeneratedApiRemover.cs'; Invocation = 'sdt.Delete'; FaultPoint = 'SdtDelete'; Route = 'Persist' },
    @{ File = 'Diagnostics\ApiPlanGeneratedApiRemover.cs'; Invocation = 'metadataFile.Delete'; FaultPoint = 'MetadataDelete'; Route = 'Persist' },
    @{ File = 'Diagnostics\ApiPlanGeneratedApiRemover.cs'; Invocation = 'folder.Delete'; FaultPoint = 'FolderDelete'; Route = 'Persist' }
)

$productionFiles = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -Filter '*.cs' -File | Select-Object -ExpandProperty FullName)

$actualCalls = New-Object System.Collections.Generic.List[object]
foreach ($path in $productionFiles) {
    $relativePath = [IO.Path]::GetRelativePath($sourceRoot, $path)
    $lineNumber = 0
    foreach ($line in Get-Content -LiteralPath $path) {
        $lineNumber++
        $trimmed = $line.Trim()
        if ($trimmed.StartsWith('//', [StringComparison]::Ordinal) -or $line.Contains('"')) {
            continue
        }

        foreach ($match in [regex]::Matches($line, '\b([A-Za-z_][A-Za-z0-9_]*)\.(Save|Delete)\b')) {
            $actualCalls.Add([PSCustomObject]@{
                    File = $relativePath
                    Invocation = "$($match.Groups[1].Value).$($match.Groups[2].Value)"
                    Line = $lineNumber
                })
        }
    }
}

# O reconhecedor é textual e casa `<qualificador>.Save|Delete`. Os enums do diário B111/F3
# têm membros chamados `Save` e `Delete` — `JournalReceiptOperation.Delete` é um valor, não
# uma chamada física. Descartá-los aqui mantém a sentinela medindo o que ela existe para
# medir, sem afrouxar o inventário de chamadas reais.
[void]$actualCalls.RemoveAll([Predicate[object]]{
        param($call)
        $call.Invocation -match '^Journal[A-Za-z]+\.(Save|Delete)$'
    })

$allowedOutsideSeam = @(
    @{ File = 'Package.cs'; Invocation = 'PrototypeWizardPreferencesStore.Save' },
    @{ File = 'PrototypeWizardPreferencesDialog.cs'; Invocation = '_texts.Save' },
    @{ File = 'Diagnostics\PrototypeWizardPreferences.cs'; Invocation = 'file.Save' },
    # O diário durável B111/F3 não passa pelo Persist(...) da F2 por decisão de contrato: ele
    # tem rotina própria de durabilidade, confirmada por FileId, bytes e hash, e atualiza
    # `journalDurability` separadamente dos recibos dos objetos de negócio. Contar o Save do
    # diário como persistência de negócio misturaria as duas contagens.
    @{ File = 'Diagnostics\ApiPlanOperationJournalStore.cs'; Invocation = 'file.Save' },
    # B120 A2: Delete do SDT ListResponse órfão fica fora do Persist(...) porque o diário do
    # Apply não admite inventory[].action=Delete (schema V1). Confirmação só por GetAll.
    @{ File = 'Diagnostics\ApiPlanListResponseOrphanCleanup.cs'; Invocation = 'sdt.Delete' }
)
foreach ($allowed in $allowedOutsideSeam) {
[void]$actualCalls.RemoveAll([Predicate[object]]{
            param($call)
            $call.File -eq $allowed.File -and $call.Invocation -eq $allowed.Invocation
        })
}

Assert-Equal $expectedCalls.Count $actualCalls.Count 'Inventário F2 de chamadas físicas mudou; classifique a nova chamada ou remova a antiga.'

foreach ($actual in $actualCalls) {
    $expected = @($expectedCalls | Where-Object {
            $_.File -eq $actual.File -and $_.Invocation -eq $actual.Invocation
        })
    Assert-Equal 1 $expected.Count "Chamada física fora do inventário F2: $($actual.File):$($actual.Line) $($actual.Invocation)."
}

foreach ($expected in $expectedCalls) {
    $matches = @($actualCalls | Where-Object {
            $_.File -eq $expected.File -and $_.Invocation -eq $expected.Invocation
        })
    Assert-Equal 1 $matches.Count "Chamada física ausente ou duplicada: $($expected.File) $($expected.Invocation)."
}

foreach ($group in @($expectedCalls | Group-Object File)) {
    $source = Get-Content -Raw -LiteralPath (Join-Path $sourceRoot $group.Name)
    $persisted = @($group.Group | Where-Object Route -eq 'Persist')
    $executed = @($group.Group | Where-Object Route -eq 'Executor')

    foreach ($call in $group.Group) {
        Assert-True ($source.Contains("PersistenceFaultPoint.$($call.FaultPoint)")) "$($group.Name) deve declarar o ponto $($call.FaultPoint)."
    }

    if ($persisted.Count -gt 0) {
        $persistCount = [regex]::Matches($source, 'ApiPlanSaveBoundaryProbe\.Persist\(').Count
        Assert-Equal $persisted.Count $persistCount "$($group.Name) deve ter um Persist por chamada física direta."
        Assert-True (-not $source.Contains('ApiPlanSaveStepExecutor.Execute(saveSteps')) "$($group.Name) não pode instrumentar o mesmo ponto também pelo executor."
    }

    if ($executed.Count -gt 0) {
        Assert-True ($source.Contains('ApiPlanSaveStepExecutor.Execute(saveSteps')) "$($group.Name) deve encaminhar os passos físicos pelo executor único."
        Assert-Equal 0 ([regex]::Matches($source, 'ApiPlanSaveBoundaryProbe\.Persist\(').Count) "$($group.Name) não pode instrumentar os passos também diretamente."
    }
}

$executorSource = Get-Content -Raw -LiteralPath (Join-Path $sourceRoot 'Diagnostics\ApiPlanSaveStepExecutor.cs')
Assert-True ($executorSource.Contains('ApiPlanSaveBoundaryProbe.Persist(')) 'O executor único deve encaminhar cada ApiPlanSaveStep ao seam comum.'

# B109 ramo C: confirmação ilegível por exceção precisa preservar a exceção. A forma antiga
# guardava só tipo + mensagem da camada externa, e uma TargetInvocationException chegava à
# Output sem a causa real. Toda exceção «não foi confirmada» repassa a causa como inner.
foreach ($path in $productionFiles) {
    $relativePath = [IO.Path]::GetRelativePath($sourceRoot, $path)
    $source = Get-Content -Raw -LiteralPath $path
    Assert-True (-not [regex]::IsMatch($source, 'PersistenceConfirmation\.Unreadable\(\s*exception\.GetType\(\)')) "$relativePath descarta a cadeia da exceção na confirmação; use PersistenceConfirmation.Unreadable(exception)."
    foreach ($match in [regex]::Matches($source, '(?s)throw new InvalidOperationException\(\s*\$"Persist[^"]*não foi confirmada:[^"]*",\s*([A-Za-z_.]+)\);')) {
        Assert-True ($match.Groups[1].Value -in @('receipt.ConfirmationCause', 'confirmation.Cause')) "$relativePath lança «não foi confirmada» sem repassar a causa: $($match.Groups[1].Value)."
    }
    $unconfirmedThrows = [regex]::Matches($source, 'throw new InvalidOperationException\(\s*\$"Persist[^"]*não foi confirmada:').Count
    $withCause = [regex]::Matches($source, '(?s)throw new InvalidOperationException\(\s*\$"Persist[^"]*não foi confirmada:[^"]*",\s*(receipt\.ConfirmationCause|confirmation\.Cause)\);').Count
    Assert-Equal $unconfirmedThrows $withCause "$relativePath tem exceção «não foi confirmada» sem a causa da confirmação como inner."
}

# B109: o inner só serve se alguém o imprime. Todo catch de etapa em Package.cs que registra
# bloqueio no relatório chama a B109ExceptionProbe. Em 2026-09-25 a etapa de SDTs não chamava,
# e a primeira ocorrência depois da correção da captura chegou sem stack. O preflight agregado
# fica fora: seus bloqueios são validações esperadas, não exceções do SDK.
$packageLines = @(Get-Content -LiteralPath (Join-Path $sourceRoot 'Package.cs'))
for ($index = 0; $index -lt $packageLines.Count; $index++) {
    if ($packageLines[$index] -notmatch 'catch \(Exception') { continue }
    $last = [Math]::Min($index + 10, $packageLines.Count - 1)
    $block = $packageLines[$index..$last] -join "`n"
    $end = $block.IndexOf('return ', [StringComparison]::Ordinal)
    if ($end -ge 0) { $block = $block.Substring(0, $end) }
    if ($block -notmatch 'report\??\.AddBlocked\(') { continue }
    if ($block -match 'Preflight agregado bloqueou') { continue }
    Assert-True ($block -match 'B109ExceptionProbe\.Describe\(') "Package.cs:$($index + 1) registra bloqueio de etapa sem publicar a cadeia pela B109ExceptionProbe."
}

Write-Output 'PASS: ApiPlanPersistenceSeamCoverage'
