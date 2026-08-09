#requires -Version 7.4
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$helperPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanCollisionConflict.cs'
$runtimeAssemblies = @([System.AppContext]::GetData('TRUSTED_PLATFORM_ASSEMBLIES') -split [System.IO.Path]::PathSeparator |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($runtimeAssemblies.Count -eq 0) {
    throw 'Assemblies do runtime PowerShell atual nao foram encontrados.'
}

Add-Type -Path $helperPath -ReferencedAssemblies @($runtimeAssemblies | Sort-Object -Unique)

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

$single = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanCollisionConflict]::new(
    'sdtContrato_API_Response',
    'SDT',
    'Root Module',
    'ContratoOpenApi')
Assert-Equal "Nome='sdtContrato_API_Response' | Tipo='SDT' | Modulo='Root Module' | Folder='ContratoOpenApi'" $single.FormatLine() 'Linha unica de conflito.'

$fileConflict = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanCollisionConflict]::new(
    'apiContrato_Metadata',
    'File',
    'Entities',
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanCollisionConflict]::NotApplicable)
Assert-True ($fileConflict.FormatLine() -match "Folder='\(n/a\)'") 'File deve usar Folder (n/a).'

$listItems = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanCollisionConflict[]]@(
    $single,
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanCollisionConflict]::new('apiContrato', 'API Object', 'Root Module', 'ContratoOpenApi')
)
$list = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanCollisionConflict]::FormatList($listItems)
Assert-True ($list.StartsWith('Conflitos (2):')) 'Cabecalho da lista deve citar a quantidade.'
Assert-True ($list -match "(?m)^  - Nome='sdtContrato_API_Response'") 'Lista deve incluir o SDT.'
Assert-True ($list -match "(?m)^  - Nome='apiContrato'") 'Lista deve incluir o API Object.'

$empty = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanCollisionConflict]::FormatList(
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanCollisionConflict[]]@())
Assert-Equal '' $empty 'Lista vazia deve ser string vazia.'

$groupA = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanCollisionConflict[]]@($single)
$groupB = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanCollisionConflict[]]@(
    [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanCollisionConflict]::new('procContrato_API_List', 'Procedure', 'Root Module', 'ContratoOpenApi'))
$merged = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanCollisionConflict]::Merge($groupA, $groupB)
Assert-Equal 2 $merged.Count 'Merge deve concatenar grupos.'

Write-Host 'Test-ApiPlanCollisionConflict.ps1 OK'
