#requires -Version 7.4
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot '../../Src/Extension/Diagnostics/ApiPlanExistingNamePolicy.cs')
$guid = [Guid]::NewGuid()
$policy = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanExistingNamePolicy]
if ($policy::IsRenameBlocked($true,$guid,'apiEmpresa','APIEMPRESA')) { throw 'Caixa diferente é o mesmo nome.' }
if (-not $policy::IsRenameBlocked($true,$guid,'apiEmpresa','apiOutra')) { throw 'Renomeação deve bloquear.' }
if ($policy::IsRenameBlocked($false,$null,$null,'apiNova')) { throw 'Primeira geração tem nome livre.' }
if ($policy::IsRenameBlocked($true,$null,'apiEmpresa','apiOutra')) { throw 'Sem GUID resolvido, não há guarda de renomeação.' }
Write-Output 'PASS: ApiPlanExistingNamePolicy'
