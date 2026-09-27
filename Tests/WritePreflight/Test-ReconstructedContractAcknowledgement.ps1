#requires -Version 7.4
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot '../../Src/Extension/Diagnostics/ReconstructedContractAcknowledgement.cs')
$type = [GenexusOpenApiBuilder.Extension.Diagnostics.ReconstructedContractAcknowledgement]
$expected = [string[]]@('fields.listFilters','api.servicesBasePath')
$ack = $type::new([string[]]@('api.servicesBasePath','fields.listFilters'), [string[]]@('fields.listFilters'))
if (-not $type::IsAccepted($true,$false,$false,$expected,$ack)) { throw 'Mesmo conjunto, inclusive seção editada, deve permitir.' }
if ($type::IsAccepted($true,$false,$false,$expected,$null)) { throw 'Sem confirmação deve bloquear.' }
$wrong = $type::new([string[]]@('fields.listFilters'))
if ($type::IsAccepted($true,$false,$false,$expected,$wrong)) { throw 'Conjunto divergente deve bloquear.' }
if ($type::IsAccepted($true,$false,$true,$expected,$ack)) { throw 'Hierarquia sem levels sob T2 deve bloquear mesmo com aceite.' }
if (-not $type::IsAccepted($true,$true,$true,$expected,$ack)) { throw 'levels null explícito não equivale a ausência.' }
if (-not $type::IsAccepted($false,$false,$true,$expected,$null)) { throw 'Sem marca importada não há T2.' }
if ($ack.Sections.Count -ne 2 -or $ack.EditedSections.Count -ne 1) { throw 'Edição não retira seção confirmada.' }
Write-Output 'PASS: ReconstructedContractAcknowledgement'
