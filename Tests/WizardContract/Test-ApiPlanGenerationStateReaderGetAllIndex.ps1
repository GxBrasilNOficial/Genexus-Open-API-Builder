Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$sourcePath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanGenerationStateReader.cs'
if (-not (Test-Path -LiteralPath $sourcePath)) {
    throw "SOURCE_MISSING: $sourcePath"
}

$source = [IO.File]::ReadAllText($sourcePath)

function Assert-Contains {
    param([string]$Text, [string]$Needle, [string]$Message)
    if ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "ASSERT_CONTAINS_FAILED: $Message"
    }
}

function Assert-NotContains {
    param([string]$Text, [string]$Needle, [string]$Message)
    if ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) {
        throw "ASSERT_NOT_CONTAINS_FAILED: $Message"
    }
}

Assert-Contains $source 'KbObjectNameIndex.Create' 'Read deve construir indice unico por tipo.'
Assert-Contains $source 'SDT.GetAll(designModel).ToLookup' 'SDT.GetAll deve ocorrer uma vez no indice.'
Assert-Contains $source 'Procedure.GetAll(designModel).ToLookup' 'Procedure.GetAll deve ocorrer uma vez no indice.'
Assert-Contains $source 'index.FindSdts(definition.Name)' 'Inspecao de SDT deve usar o indice.'
Assert-Contains $source 'index.FindProcedures(name)' 'Inspecao de Procedure deve usar o indice.'

$inspectSdtsStart = $source.IndexOf('private static ApiPlanGenerationInspection InspectSdts(', [StringComparison]::Ordinal)
$inspectProceduresStart = $source.IndexOf('private static ApiPlanGenerationInspection InspectProcedures(', [StringComparison]::Ordinal)
if ($inspectSdtsStart -lt 0 -or $inspectProceduresStart -lt 0) {
    throw 'ASSERT_FAILED: metodos InspectSdts/InspectProcedures nao encontrados.'
}

$inspectSdts = $source.Substring($inspectSdtsStart, $inspectProceduresStart - $inspectSdtsStart)
Assert-NotContains $inspectSdts 'SDT.GetAll(' 'InspectSdts nao deve chamar SDT.GetAll.'

$inspectApiStart = $source.IndexOf('private static ApiPlanGenerationInspection InspectApiObject(', [StringComparison]::Ordinal)
$inspectProcedures = $source.Substring($inspectProceduresStart, $inspectApiStart - $inspectProceduresStart)
Assert-NotContains $inspectProcedures 'Procedure.GetAll(' 'InspectProcedures nao deve chamar Procedure.GetAll.'

Write-Output 'PASS: ApiPlanGenerationStateReaderGetAllIndex'
