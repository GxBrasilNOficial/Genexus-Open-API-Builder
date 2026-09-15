#requires -Version 7.4

# Exerce o catálogo de saída **contra si mesmo**, em vez de por amostra.
#
# O gate irmão, `Test-ExtensionOutputLocalization.ps1`, escolhe frases e confere `Contains`. Ele é
# necessário — prova que a tradução certa chega ao texto certo — e insuficiente: só vê as frases
# que alguém lembrou de escrever nele. Em 2026-09-15 ele estava verde com **oito frases
# corrompidas** dentro do catálogo.
#
# A pergunta aqui é a mais simples que se pode fazer a um catálogo de substituição por substring:
# para cada entrada, `Translate(Source, idioma)` devolve a tradução que aquela entrada cadastrou?
#
# Se não devolver, alguma outra entrada está recortando esta — e toda mensagem real que contenha
# este `Source` sai meio em cada idioma. É o pior modo de falha para texto: não quebra, não lança,
# e produz frase plausível. Um leitor espanhol não tem como saber se `Arquivo de metadatos:` é
# defeito ou termo que a ferramenta deixou em português de propósito.
#
# O gate também recusa `Source` duplicado. A substituição é sequencial e a primeira vence, então
# a segunda é código morto com cara de configuração. **Atenção ao comparar:** `Nenhum` e `nenhum`
# são `Source` distintos e ambos legítimos — o catálogo cadastra pares de capitalização de
# propósito, um para início de frase e outro para o meio. Comparação ordinal, nunca hashtable do
# PowerShell, que é case-insensitive por padrão e funde os dois.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$languagePath = Join-Path $PSScriptRoot '..\..\Src\Domain\ExtensionLanguage.cs'
$outputLocalizationPath = Join-Path $PSScriptRoot '..\..\Src\Domain\ExtensionOutputLocalization.cs'
$runtimeAssemblies = @([System.AppContext]::GetData('TRUSTED_PLATFORM_ASSEMBLIES') -split [System.IO.Path]::PathSeparator |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

Add-Type -Path @($languagePath, $outputLocalizationPath) -ReferencedAssemblies $runtimeAssemblies

$languageType = [AppDomain]::CurrentDomain.GetAssemblies() |
    ForEach-Object { $_.GetType('GenexusOpenApiBuilder.Extension.Domain.ExtensionLanguage', $false) } |
    Where-Object { $null -ne $_ } |
    Select-Object -First 1
$localizerType = [AppDomain]::CurrentDomain.GetAssemblies() |
    ForEach-Object { $_.GetType('GenexusOpenApiBuilder.Extension.Domain.ExtensionOutputLocalization', $false) } |
    Where-Object { $null -ne $_ } |
    Select-Object -First 1

if ($null -eq $languageType) { throw 'ASSERT_FAILED: o enum de idioma deve ser carregado.' }
if ($null -eq $localizerType) { throw 'ASSERT_FAILED: o localizador do Output deve ser carregado.' }

$translate = $localizerType.GetMethod('Translate')
$ptBr = [Enum]::Parse($languageType, 'PortugueseBrazil')
$spanish = [Enum]::Parse($languageType, 'Spanish')
$english = [Enum]::Parse($languageType, 'English')

$phrasesField = $localizerType.GetField('Phrases', [System.Reflection.BindingFlags]'NonPublic, Static')
if ($null -eq $phrasesField) { throw 'ASSERT_FAILED: o array Phrases deve existir para ser exercido.' }

$phrases = $phrasesField.GetValue($null)
if ($phrases.Length -lt 100) { throw "ASSERT_FAILED: catálogo pequeno demais para ser o real ($($phrases.Length))." }

$phraseType = $phrases.GetValue(0).GetType()
$sourceProperty = $phraseType.GetProperty('Source')
$spanishProperty = $phraseType.GetProperty('Spanish')
$englishProperty = $phraseType.GetProperty('English')

$failures = New-Object System.Collections.Generic.List[string]
$seen = New-Object 'System.Collections.Generic.Dictionary[string,int]' ([StringComparer]::Ordinal)

for ($index = 0; $index -lt $phrases.Length; $index++) {
    $phrase = $phrases.GetValue($index)
    $source = $sourceProperty.GetValue($phrase)
    $expectedSpanish = $spanishProperty.GetValue($phrase)
    $expectedEnglish = $englishProperty.GetValue($phrase)

    if ([string]::IsNullOrEmpty($source)) {
        $failures.Add("idx=${index}: Source vazio.")
        continue
    }

    if ($seen.ContainsKey($source)) {
        $failures.Add("idx=${index}: Source duplicado — já cadastrado em idx=$($seen[$source]). A segunda entrada nunca executa: [$source]")
    }
    else {
        $seen.Add($source, $index)
    }

    # Português é a língua de origem: o catálogo não pode tocá-la.
    $portuguese = $translate.Invoke($null, [object[]] @($source, $ptBr))
    if ($portuguese -cne $source) {
        $failures.Add("idx=${index} [pt-BR]: a origem foi alterada. esperado=[$source] obtido=[$portuguese]")
    }

    $actualSpanish = $translate.Invoke($null, [object[]] @($source, $spanish))
    if ($actualSpanish -cne $expectedSpanish) {
        $failures.Add("idx=${index} [es]: src=[$source] esperado=[$expectedSpanish] obtido=[$actualSpanish]")
    }

    $actualEnglish = $translate.Invoke($null, [object[]] @($source, $english))
    if ($actualEnglish -cne $expectedEnglish) {
        $failures.Add("idx=${index} [en]: src=[$source] esperado=[$expectedEnglish] obtido=[$actualEnglish]")
    }
}

if ($failures.Count -gt 0) {
    $detail = ($failures | Select-Object -First 25) -join [Environment]::NewLine
    $extra = if ($failures.Count -gt 25) { [Environment]::NewLine + "... e mais $($failures.Count - 25)." } else { '' }
    throw "ASSERT_FAILED: catálogo inconsistente consigo mesmo — $($failures.Count) ocorrência(s).$([Environment]::NewLine)$detail$extra"
}

Write-Output "PASS: ExtensionOutputLocalizationSelfConsistency ($($phrases.Length) entradas, 0 inconsistências)"
