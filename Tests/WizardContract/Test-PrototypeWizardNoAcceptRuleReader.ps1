#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$readerPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\PrototypeWizardNoAcceptRuleReader.cs'
if (-not (Test-Path -LiteralPath $readerPath)) {
    throw "SOURCE_MISSING: $readerPath"
}

Add-Type -Path $readerPath
$readerType = [System.AppDomain]::CurrentDomain.GetAssemblies() |
    ForEach-Object { $_.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.PrototypeWizardNoAcceptRuleReader', $false) } |
    Where-Object { $null -ne $_ } |
    Select-Object -First 1
if ($null -eq $readerType) {
    throw 'TIPO_NAO_CARREGADO: PrototypeWizardNoAcceptRuleReader'
}

$method = $readerType.GetMethod('ReadAttributeNames', [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static)
if ($null -eq $method) {
    throw 'METODO_NAO_CARREGADO: ReadAttributeNames'
}

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw "ASSERT_TRUE_FAILED: $Message"
    }
}

$rules = @'
// noaccept(CommentedLine);
/*
noaccept(CommentedBlock);
*/
[web]
{
    noaccept(EmployeeAddedDate);
    NoAccept(employeeaddeddate) if insert;
}
msg(!"texto noaccept(IgnoredString)");
'@

$names = @($method.Invoke($null, @($rules)))
Assert-True ($names.Count -eq 1) 'Regra NoAccept deve ser detectada uma vez e deduplicada sem diferenciar maiusculas.'
Assert-True ($names[0] -eq 'EmployeeAddedDate') 'Atributo coberto por NoAccept deve ser identificado.'

$empty = @($method.Invoke($null, @('   ')))
Assert-True ($empty.Count -eq 0) 'Rules vazias nao devem produzir atributos bloqueados.'

Write-Output 'PASS: PrototypeWizardNoAcceptRuleReader'
