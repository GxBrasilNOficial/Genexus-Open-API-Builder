# Probe de contrato: membro colecao em SDT apos Save() + specifier.
# Evidencia IDE 2026-09-03: ErrorResponse e ListResponse continuaram
# Reencountered apos tolerar CollectionItemName vazio e apos ler o tipo
# via CollectionItemName. O specifier troca CollectionItemName pelo nome
# do item (Messages, Items), nao pelo tipo sdt_*.

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$sdtWriter = Join-Path $repoRoot 'Src\Extension\Diagnostics\ApiPlanSdtWriter.cs'

function Assert-Contains([string]$Haystack, [string]$Needle, [string]$Message) {
    if ($Haystack -notmatch [regex]::Escape($Needle)) {
        throw "FAIL: $Message"
    }
}

if (-not (Test-Path -LiteralPath $sdtWriter)) {
    throw "Arquivo nao encontrado: $sdtWriter"
}

$content = Get-Content -LiteralPath $sdtWriter -Raw
Assert-Contains $content 'CollectionItemNameMatches' 'Writer deve centralizar o match de CollectionItemName.'
Assert-Contains $content 'string.IsNullOrWhiteSpace(item.CollectionItemName)' 'Match deve tolerar CollectionItemName vazio apos Save.'
Assert-Contains $content 'TryResolveStructureTypeReferenceName' 'Match deve resolver StructureTypeReference (Id) para o nome do SDT.'
Assert-Contains $content 'NormalizeSdtTypeName' 'Match de tipo SDT deve normalizar sdt:Nome, Modulo.'

Write-Output 'PASS: SdtCollectionItemNameProbe'
