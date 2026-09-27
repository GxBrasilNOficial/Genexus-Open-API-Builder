#requires -Version 7.4
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$json = Get-ChildItem (Join-Path $env:USERPROFILE '.nuget/packages/newtonsoft.json') -Filter Newtonsoft.Json.dll -Recurse | Where-Object FullName -match '[\\/]lib[\\/]netstandard2.0[\\/]Newtonsoft.Json.dll$' | Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
$runtime = @(Get-ChildItem (Join-Path $PSHOME 'ref') -Filter '*.dll' | Select-Object -ExpandProperty FullName)
Add-Type -Path (Join-Path $PSScriptRoot '../../Src/Extension/Diagnostics/ApiPlanContractProvenance.cs') -ReferencedAssemblies @(($runtime + $json) | Sort-Object -Unique)
$type = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanContractProvenance]
function Read-Provenance([string]$Text) { $type::Read([Newtonsoft.Json.Linq.JObject]::Parse($Text),$true,$true,$true,$true,$true,$true,$true,$true) }
$full = Read-Provenance '{"schemaVersion":"GOAB_API_METADATA_V4","services":[],"descriptions":{"services":[]},"security":{"level":"None"},"api":{"restPath":"/x","servicesBasePath":"apiX"},"fields":{"createRequest":[],"updateRequest":[],"response":[],"listFilters":[],"required":[]},"pagination":{"defaultPageSize":50,"maximumPageSize":200},"order":[],"levels":null,"errorDetail":{"includeBusinessComponentMessages":true}}'
if ($full.ReconstructedSections.Count -ne 0 -or $full.Imported) { throw 'Completa deve ser Metadata, sem T2.' }
$recovered = Read-Provenance '{"recovery":{"imported":true,"notRecovered":[]}}'
if (-not $recovered.Imported -or $recovered.HasLevelsKey) { throw 'B115 dispara T2 com levels ausente.' }
foreach ($key in @('api.servicesBasePath','errorDetail.includeBusinessComponentMessages','fields.required','order','levels')) {
    if ($recovered.Sections[$key].ToString() -ne 'Default') { throw "Padrão fixo esperado: $key" }
}
if ($recovered.Sections['fields.listFilters'].ToString() -ne 'Fallback') { throw 'Filtros vêm do Source.' }
$legacy = Read-Provenance '{"schemaVersion":"GOAB_API_METADATA_B060_V1"}'
if ($legacy.Imported) { throw 'Metadata legada não dispara T2.' }
$nullLevels = Read-Provenance '{"recovery":{"imported":true},"levels":null}'
if (-not $nullLevels.HasLevelsKey -or $nullLevels.Sections['levels'].ToString() -ne 'Metadata') { throw 'Null explícito é distinto de ausência.' }
$attributes = [string[]]@('EmpresaId','Data','Valor')
if ($type::ReadFilters('', $attributes).Count -ne 0) { throw 'List() B054/B055 deve reconstruir zero filtros.' }
$filters = $type::ReadFilters('in:&EmpresaId, in:&DataFrom, in:&DataTo, in:&ValorMin, in:&ValorMax, in:&PageNumber, out:&ListResponse', $attributes)
if ($filters.Count -ne 3 -or $filters[0].Name -ne 'EmpresaId' -or -not $filters[1].UsesPeriod -or -not $filters[2].UsesRange) { throw 'Source B070 deve reconstruir os filtros, sem paginação/output.' }
Write-Output 'PASS: ApiPlanContractProvenance e gatilho T2'
