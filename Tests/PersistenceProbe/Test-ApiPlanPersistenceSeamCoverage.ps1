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

$allowedOutsideSeam = @(
    @{ File = 'Package.cs'; Invocation = 'PrototypeWizardPreferencesStore.Save' },
    @{ File = 'PrototypeWizardPreferencesDialog.cs'; Invocation = '_texts.Save' },
    @{ File = 'Diagnostics\PrototypeWizardPreferences.cs'; Invocation = 'file.Save' }
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

Write-Output 'PASS: ApiPlanPersistenceSeamCoverage'
