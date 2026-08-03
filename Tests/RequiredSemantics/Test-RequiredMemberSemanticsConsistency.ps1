#requires -Version 7.4
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Guarda a coerencia entre a semantica de Required implementada no codigo gerado e os textos que
# a descrevem para o usuario. O GeneXus nao expoe presenca de membro JSON sem comando csharp, entao
# Create/Update validam preenchimento: 400 quando o obrigatorio chega ausente ou com o valor default
# do tipo. Textos que ainda falem em presenca, ou que reduzam a limitacao a "ausente ou vazio",
# passam a ideia errada de que 0 e false seriam aceitos.
#
# Este teste existe porque a divergencia ja reapareceu duas vezes: build e testes passam com o texto
# errado, entao nenhuma outra checagem mecanica pega a regressao.

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

$canonicalPhrase = 'valor default do tipo (vazio, false ou 0)'

# Frases proibidas em superficies vivas: codigo de UI e mensagens de Output.
$forbiddenInLiveSurfaces = @(
    'presença do membro',
    'presenca do membro',
    'exige presença',
    'exige presenca',
    'não exige valor não vazio',
    'nao exige valor nao vazio',
    'nao valor nao-vazio',
    'continuam valores enviados',
    'ausente ou vazio'
)

# Em documentacao, apenas a reducao imprecisa e proibida. As transcricoes historicas de Output em
# Docs/Implementation/B03x preservam deliberadamente o texto antigo e nao entram neste escopo.
$forbiddenInDocuments = @(
    'ausente ou vazio'
)

$liveSurfaces = @(
    'Src\Extension\PrototypeWizardDialog.cs',
    'Src\Extension\Package.cs'
)

$documents = @(
    'CHANGELOG.md',
    'Docs\STATUS_ATUAL_E_PROXIMO_PASSO.md',
    'Docs\Implementation\B071-B073-B079-GET-CREATE-UPDATE-HTTP.md'
)

function Get-RepositoryFileText {
    param([string]$RelativePath)

    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "ASSERT_FILE_MISSING: arquivo esperado nao encontrado: '$RelativePath'."
    }

    return [System.IO.File]::ReadAllText($path)
}

function Assert-NoForbiddenPhrase {
    param([string]$RelativePath, [string[]]$Phrases)

    $text = Get-RepositoryFileText -RelativePath $RelativePath
    $lines = $text -split "`n"
    foreach ($phrase in $Phrases) {
        for ($index = 0; $index -lt $lines.Count; $index++) {
            if ($lines[$index].IndexOf($phrase, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                $lineNumber = $index + 1
                throw "ASSERT_FORBIDDEN_PHRASE: '$RelativePath':$lineNumber usa a semantica antiga de Required: '$phrase'. Use '$canonicalPhrase'."
            }
        }
    }
}

function Assert-CanonicalPhrase {
    param([string]$RelativePath)

    $text = Get-RepositoryFileText -RelativePath $RelativePath
    if ($text.IndexOf($canonicalPhrase, [StringComparison]::Ordinal) -lt 0) {
        throw "ASSERT_CANONICAL_MISSING: '$RelativePath' deixou de explicar a semantica de Required com '$canonicalPhrase'."
    }
}

foreach ($relativePath in $liveSurfaces) {
    Assert-NoForbiddenPhrase -RelativePath $relativePath -Phrases $forbiddenInLiveSurfaces
    Assert-CanonicalPhrase -RelativePath $relativePath
}

foreach ($relativePath in $documents) {
    Assert-NoForbiddenPhrase -RelativePath $relativePath -Phrases $forbiddenInDocuments
}

# A mensagem de erro do runtime tambem faz parte do contrato comunicado ao consumidor da API.
# O recorte isola a geracao atual das variantes preservadas apenas para migracao, que mantem
# deliberadamente o texto antigo.
$writerSource = Get-RepositoryFileText -RelativePath 'Src\Extension\Diagnostics\ApiPlanBusinessComponentWriter.cs'
$currentStart = $writerSource.IndexOf('private static IEnumerable<string> DefaultValueRequiredMemberValidation', [StringComparison]::Ordinal)
$previousStart = $writerSource.IndexOf('private static IEnumerable<string> PreviousB079SdtDirtyMemberPresenceValidation', [StringComparison]::Ordinal)
if ($currentStart -lt 0 -or $previousStart -lt 0 -or $previousStart -le $currentStart) {
    throw 'ASSERT_SECTION_FAILED: nao foi possivel isolar DefaultValueRequiredMemberValidation atual.'
}

$currentSource = $writerSource.Substring($currentStart, $previousStart - $currentStart)
if ($currentSource.IndexOf('Required JSON member(s) missing or empty', [StringComparison]::Ordinal) -lt 0) {
    throw 'ASSERT_RUNTIME_MESSAGE: a mensagem de 400 gerada deve declarar "missing or empty", nao apenas "missing".'
}

Write-Output 'PASS: RequiredMemberSemanticsConsistency'
