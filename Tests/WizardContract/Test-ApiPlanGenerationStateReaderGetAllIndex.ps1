Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$sourcePath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanGenerationStateReader.cs'
$indexPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanKbObjectNameIndex.cs'
$sdtWriterPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanSdtWriter.cs'
foreach ($path in @($sourcePath, $indexPath, $sdtWriterPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "SOURCE_MISSING: $path"
    }
}

$source = [IO.File]::ReadAllText($sourcePath)
$indexSource = [IO.File]::ReadAllText($indexPath)
$sdtWriter = [IO.File]::ReadAllText($sdtWriterPath)

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

Assert-Contains $source 'ApiPlanKbObjectNameIndex.Create' 'Read deve construir indice unico por tipo.'
Assert-Contains $indexSource 'SDT.GetAll(designModel).ToLookup' 'SDT.GetAll deve ocorrer uma vez no indice.'
Assert-Contains $indexSource 'Procedure.GetAll(designModel).ToLookup' 'Procedure.GetAll deve ocorrer uma vez no indice.'
Assert-Contains $indexSource 'internal void RefreshFolders' 'Indice deve reindexar pastas apos criar GxOpenAPI.'
Assert-Contains $indexSource 'internal void RefreshSdts' 'Indice deve reindexar SDTs apos gravacao.'
Assert-Contains $sdtWriter 'kbIndex.RefreshFolders(designModel)' 'Apos CreateSharedFolder o indice de pastas nao pode ficar stale.'
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
