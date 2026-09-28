# Funções puras compartilhadas pelos checks B129 do checker pré-push e pelo meta-teste.
# Não iniciam processo nem gravam nada; a leitura de arquivo se limita ao .props de referências
# e à verificação de presença das referências pinadas.
Set-StrictMode -Version Latest

$script:B129FrameworkReferences = @('System.Drawing', 'System.Windows.Forms')
$script:B129HeaderEnglish = [regex]::new('Found conflicts between different versions of "(?<asm>[^"]+)"', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
$script:B129HeaderPortuguese = [regex]::new('foram encontrados conflitos entre diferentes versões do "(?<asm>[^"]+)"', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
$script:B129Msb3277Line = [regex]::new('warning MSB3277:', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
$script:B129OtherWarningLine = [regex]::new(': warning( [A-Z]+[0-9]+)? ?:', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
$script:B129CountLine = [regex]::new('^\d+\s+(Warning|Error|Aviso|Erro)\(s\)\s*$', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
$script:B129ErrorCode = [regex]::new('\berror (?<code>[A-Z]+[0-9]+)\s*:', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

function Test-B129PathUnder {
    param([string]$Path, [string]$Root)
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd([char]'\', [char]'/') + [System.IO.Path]::DirectorySeparatorChar
    return $fullPath.StartsWith($fullRoot, [System.StringComparison]::OrdinalIgnoreCase)
}

# --- Paridade dos itens Compile (seção 4.1) ---

function Read-B129MsbuildJson {
    param([AllowNull()] [string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text)) { return $null }
    try { return ($Text | ConvertFrom-Json -Depth 64) }
    catch { return $null }
}

function Get-B129TargetFramework {
    param([AllowNull()] [string]$Text)
    $json = Read-B129MsbuildJson $Text
    if ($null -eq $json) { return [pscustomobject]@{ Readable = $false; Target = $null } }
    $properties = $json.PSObject.Properties['Properties']
    if ($null -eq $properties -or $null -eq $properties.Value) { return [pscustomobject]@{ Readable = $false; Target = $null } }
    $single = $properties.Value.PSObject.Properties['TargetFramework']
    $multiple = $properties.Value.PSObject.Properties['TargetFrameworks']
    $target = $null
    if ($null -ne $single -and -not [string]::IsNullOrWhiteSpace([string]$single.Value)) { $target = ([string]$single.Value).Trim() }
    elseif ($null -ne $multiple -and -not [string]::IsNullOrWhiteSpace([string]$multiple.Value)) {
        $target = @(([string]$multiple.Value) -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' })[0]
    }
    return [pscustomobject]@{ Readable = ($null -ne $target); Target = $target }
}

function Get-B129CompileItems {
    param([AllowNull()] [string]$Text, [string]$SrcRoot)
    $json = Read-B129MsbuildJson $Text
    if ($null -eq $json) { return [pscustomobject]@{ Readable = $false; Paths = @() } }
    $paths = [System.Collections.Generic.List[string]]::new()
    $itemsProperty = $json.PSObject.Properties['Items']
    if ($null -ne $itemsProperty -and $null -ne $itemsProperty.Value) {
        $compileProperty = $itemsProperty.Value.PSObject.Properties['Compile']
        if ($null -ne $compileProperty -and $null -ne $compileProperty.Value) {
            foreach ($item in @($compileProperty.Value)) {
                $fullPathProperty = $item.PSObject.Properties['FullPath']
                if ($null -eq $fullPathProperty -or [string]::IsNullOrWhiteSpace([string]$fullPathProperty.Value)) {
                    return [pscustomobject]@{ Readable = $false; Paths = @() }
                }
                $paths.Add((ConvertTo-B129SrcRelativePath -FullPath ([string]$fullPathProperty.Value) -SrcRoot $SrcRoot))
            }
        }
    }
    return [pscustomobject]@{ Readable = $true; Paths = @($paths) }
}

function ConvertTo-B129SrcRelativePath {
    param([string]$FullPath, [string]$SrcRoot)
    $relative = [System.IO.Path]::GetRelativePath([System.IO.Path]::GetFullPath($SrcRoot), [System.IO.Path]::GetFullPath($FullPath))
    return $relative.Replace('\', '/')
}

function Compare-B129CompileSets {
    param([string[]]$CanonicalPaths, [string[]]$SatellitePaths)
    $canonical = @($CanonicalPaths | Where-Object { $null -ne $_ })
    $satellite = @($SatellitePaths | Where-Object { $null -ne $_ })
    $satelliteLine = '^extension/line\.gx18u13/'
    $canonicalLine = '^extension/line\.gx18u14plus/'
    $forbiddenInCanonical = @($canonical | Where-Object { $_ -match $satelliteLine } | Sort-Object -Unique)
    $forbiddenInSatellite = @($satellite | Where-Object { $_ -match $canonicalLine } | Sort-Object -Unique)

    $duplicates = [System.Collections.Generic.List[object]]::new()
    foreach ($project in @(@{ Name = 'canonical'; Paths = $canonical }, @{ Name = 'satellite'; Paths = $satellite })) {
        foreach ($group in @($project.Paths | Group-Object { $_.ToLowerInvariant() } | Where-Object { $_.Count -gt 1 })) {
            $duplicates.Add([ordered]@{ project = $project.Name; path = $group.Group[0]; count = $group.Count })
        }
    }

    $canonicalKeys = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($path in $canonical) { if ($path -notmatch $satelliteLine -and $path -notmatch $canonicalLine -and -not $canonicalKeys.ContainsKey($path)) { $canonicalKeys[$path] = $path } }
    $satelliteKeys = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($path in $satellite) { if ($path -notmatch $satelliteLine -and $path -notmatch $canonicalLine -and -not $satelliteKeys.ContainsKey($path)) { $satelliteKeys[$path] = $path } }
    $onlyCanonical = @($canonicalKeys.Keys | Where-Object { -not $satelliteKeys.ContainsKey($_) } | ForEach-Object { $canonicalKeys[$_] } | Sort-Object)
    $onlySatellite = @($satelliteKeys.Keys | Where-Object { -not $canonicalKeys.ContainsKey($_) } | ForEach-Object { $satelliteKeys[$_] } | Sort-Object)

    $passed = ($onlyCanonical.Count -eq 0 -and $onlySatellite.Count -eq 0 -and $forbiddenInCanonical.Count -eq 0 -and $forbiddenInSatellite.Count -eq 0 -and $duplicates.Count -eq 0)
    return [pscustomobject]@{
        Passed = $passed
        OnlyCanonical = $onlyCanonical
        OnlySatellite = $onlySatellite
        ForbiddenInCanonical = $forbiddenInCanonical
        ForbiddenInSatellite = $forbiddenInSatellite
        Duplicates = @($duplicates)
    }
}

# --- Contrato do .props de referências e estado das refs (seção 4.2) ---

function Test-B129ReferencesContract {
    param([string]$PropsPath, [string]$ProjectDirectory, [string]$LibDirectory)
    $violations = [System.Collections.Generic.List[string]]::new()
    $expected = [System.Collections.Generic.List[string]]::new()
    if (-not (Test-Path -LiteralPath $PropsPath -PathType Leaf)) {
        $violations.Add('O .props de referências do satélite não existe.')
        return [pscustomobject]@{ Valid = $false; Violations = @($violations); ExpectedFiles = @() }
    }
    $document = [System.Xml.XmlDocument]::new()
    try { $document.Load($PropsPath) }
    catch {
        $violations.Add("O .props de referências não é XML legível: $($_.Exception.Message)")
        return [pscustomobject]@{ Valid = $false; Violations = @($violations); ExpectedFiles = @() }
    }
    $hintPathCount = 0
    foreach ($reference in @($document.SelectNodes("//*[local-name()='Reference']"))) {
        $include = $reference.GetAttribute('Include')
        $hintNode = $reference.SelectSingleNode("*[local-name()='HintPath']")
        $hintPath = if ($null -ne $hintNode) { $hintNode.InnerText.Trim() } else { '' }
        if ([string]::IsNullOrWhiteSpace($hintPath)) {
            if ($include -notin $script:B129FrameworkReferences) { $violations.Add("Reference '$include' sem HintPath fora da lista fechada de framework.") }
            continue
        }
        $hintPathCount++
        $privateNode = $reference.SelectSingleNode("*[local-name()='Private']")
        $privateValue = if ($null -ne $privateNode) { $privateNode.InnerText.Trim() } elseif ($reference.HasAttribute('Private')) { $reference.GetAttribute('Private').Trim() } else { '' }
        if (-not [string]::Equals($privateValue, 'false', [System.StringComparison]::OrdinalIgnoreCase)) { $violations.Add("Reference '$include' com HintPath sem Private=false.") }
        if ($hintPath.IndexOfAny([char[]]@('*', '?')) -ge 0) { $violations.Add("Reference '$include' com curinga no HintPath."); continue }
        if ([System.IO.Path]::IsPathRooted($hintPath)) { $violations.Add("Reference '$include' com HintPath absoluto."); continue }
        $resolved = [System.IO.Path]::GetFullPath((Join-Path $ProjectDirectory $hintPath))
        if (-not (Test-B129PathUnder -Path $resolved -Root $LibDirectory)) { $violations.Add("Reference '$include' com HintPath fora de Src/Lib/Gx18u13."); continue }
        $expected.Add($resolved)
    }
    if ($hintPathCount -eq 0) { $violations.Add('O .props de referências não declara nenhum HintPath.') }
    return [pscustomobject]@{ Valid = ($violations.Count -eq 0); Violations = @($violations); ExpectedFiles = @($expected) }
}

function Get-B129SatelliteRefsState {
    param([pscustomobject]$Contract, [string]$LibDirectory)
    if (-not (Test-Path -LiteralPath $LibDirectory -PathType Container)) { return [pscustomobject]@{ State = 'absent'; Missing = @() } }
    if (-not $Contract.Valid) { return [pscustomobject]@{ State = 'unverifiable'; Missing = @() } }
    $missing = @($Contract.ExpectedFiles | Where-Object { -not (Test-Path -LiteralPath $_ -PathType Leaf) })
    if ($missing.Count -gt 0) { return [pscustomobject]@{ State = 'incomplete'; Missing = $missing } }
    return [pscustomobject]@{ State = 'complete'; Missing = @() }
}

# --- Avisos do build satélite (seção 4.2, regras 1 a 7) ---

function Get-B129SatelliteWarnings {
    param([AllowNull()] [string]$Output)
    $groups = [System.Collections.Generic.List[object]]::new()
    $otherWarnings = [System.Collections.Generic.List[string]]::new()
    $current = $null
    $englishHeaders = 0
    $portugueseHeaders = 0
    foreach ($raw in @(([string]$Output) -split "`r?`n")) {
        $line = $raw.Trim()
        if ($script:B129CountLine.IsMatch($line)) { $current = $null; continue }
        if ($script:B129Msb3277Line.IsMatch($line)) {
            $english = $script:B129HeaderEnglish.Match($line)
            $portuguese = $script:B129HeaderPortuguese.Match($line)
            if ($english.Success -or $portuguese.Success) {
                if ($english.Success) { $englishHeaders++; $assembly = $english.Groups['asm'].Value } else { $portugueseHeaders++; $assembly = $portuguese.Groups['asm'].Value }
                $current = [pscustomobject]@{ Assembly = $assembly; Orphan = $false; Lines = [System.Collections.Generic.List[string]]::new() }
                $current.Lines.Add($line)
                $groups.Add($current)
            }
            elseif ($null -ne $current) { $current.Lines.Add($line) }
            else {
                $current = [pscustomobject]@{ Assembly = $null; Orphan = $true; Lines = [System.Collections.Generic.List[string]]::new() }
                $current.Lines.Add($line)
                $groups.Add($current)
            }
            continue
        }
        $current = $null
        if ($script:B129OtherWarningLine.IsMatch($line)) { $otherWarnings.Add("satellite-build: $line") }
    }

    $knownGroups = @($groups | Where-Object { -not $_.Orphan -and [string]::Equals($_.Assembly, 'mscorlib', [System.StringComparison]::OrdinalIgnoreCase) })
    $unknownGroups = @($groups | Where-Object { $_.Orphan -or -not [string]::Equals($_.Assembly, 'mscorlib', [System.StringComparison]::OrdinalIgnoreCase) })
    $rawEntries = [System.Collections.Generic.List[string]]::new()
    foreach ($group in $unknownGroups) { $rawEntries.Add("satellite-build: $($group.Lines[0]) ($($group.Lines.Count) linha(s) no grupo)") }
    foreach ($entry in $otherWarnings) { $rawEntries.Add($entry) }

    $entries = [System.Collections.Generic.List[string]]::new()
    $occurrences = [System.Collections.Generic.List[object]]::new()
    foreach ($group in @($rawEntries | Group-Object -CaseSensitive)) {
        $entries.Add($group.Name)
        $occurrences.Add([ordered]@{ warning = $group.Name; occurrences = $group.Count })
    }

    $language = if ($englishHeaders -gt 0 -and $portugueseHeaders -gt 0) { 'mixed' } elseif ($englishHeaders -gt 0) { 'en' } elseif ($portugueseHeaders -gt 0) { 'pt' } else { 'none' }
    $knownLines = 0
    foreach ($group in $knownGroups) { $knownLines += $group.Lines.Count }
    return [pscustomobject]@{
        Warnings = @($entries)
        Occurrences = @($occurrences)
        KnownWarnings = [ordered]@{ groups = $knownGroups.Count; headers = $knownGroups.Count; continuationLines = ($knownLines - $knownGroups.Count); totalLines = $knownLines }
        UnknownGroups = @($unknownGroups | ForEach-Object { [ordered]@{ assembly = $_.Assembly; orphan = $_.Orphan; lines = @($_.Lines) } })
        HeaderLanguage = $language
    }
}

# --- Classificação de falhas dos checks novos (seção 4.4) ---

function Get-B129FailureClassification {
    param([AllowNull()] [string]$StdOut, [AllowNull()] [string]$StdErr, [string]$RepositoryRoot)
    $lines = @((([string]$StdOut) + "`n" + ([string]$StdErr)) -split "`r?`n" |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ -match '(?i): error ' -or $_ -match '(?i)warning MSB3026' })
    $errorLines = @($lines | Where-Object { $_ -match '(?i): error ' })
    if ($errorLines.Count -eq 0) { return [pscustomobject]@{ Kind = 'unclassified'; Status = 'failed' } }

    if (@($lines | Where-Object { $_ -match '(?i)\bMSB4236\b' }).Count -gt 0) { return [pscustomobject]@{ Kind = 'sdkUnavailable'; Status = 'environmentBlocked' } }

    $importLines = @($lines | Where-Object { $_ -match '(?i)\bMSB4019\b' })
    if ($importLines.Count -gt 0) {
        $paths = foreach ($line in $importLines) {
            $match = [regex]::Match($line, '(?i)MSB4019:[^"]*"(?<path>[^"]+)"')
            if ($match.Success -and [System.IO.Path]::IsPathRooted($match.Groups['path'].Value)) { $match.Groups['path'].Value } else { $null }
        }
        $paths = @($paths)
        if (@($paths | Where-Object { $null -ne $_ -and -not (Test-B129PathUnder -Path $_ -Root $RepositoryRoot) }).Count -gt 0) { return [pscustomobject]@{ Kind = 'sdkUnavailable'; Status = 'environmentBlocked' } }
        if (@($paths | Where-Object { $null -ne $_ }).Count -gt 0) { return [pscustomobject]@{ Kind = 'repositoryImportMissing'; Status = 'failed' } }
        return [pscustomobject]@{ Kind = 'unclassified'; Status = 'failed' }
    }

    if (@($lines | Where-Object { $_ -match '(?i)Access to the path .* is denied|Acesso ao caminho .* foi negado|Access is denied|Acesso negado' }).Count -gt 0) { return [pscustomobject]@{ Kind = 'accessDenied'; Status = 'environmentBlocked' } }

    if (@($lines | Where-Object { $_ -match '(?i)\bMSB3027\b' -or ($_ -match '(?i)\bMSB3021\b' -and $_ -match '(?i)being used by another process|sendo usado por outro processo') }).Count -gt 0) { return [pscustomobject]@{ Kind = 'fileLocked'; Status = 'environmentBlocked' } }

    foreach ($line in $errorLines) {
        $code = $script:B129ErrorCode.Match($line)
        $codeValue = if ($code.Success) { $code.Groups['code'].Value.ToUpperInvariant() } else { '' }
        if ($codeValue -eq 'NETSDK1045' -or ($codeValue -eq '' -and $line -match '(?i)A compatible installed \.NET SDK|compatible \.NET SDK was not found')) { return [pscustomobject]@{ Kind = 'sdkUnavailable'; Status = 'environmentBlocked' } }
    }
    foreach ($line in $errorLines) {
        $code = $script:B129ErrorCode.Match($line)
        $codeValue = if ($code.Success) { $code.Groups['code'].Value.ToUpperInvariant() } else { '' }
        if ($codeValue -eq 'NU1301') { return [pscustomobject]@{ Kind = 'networkOrFeedUnavailable'; Status = 'environmentBlocked' } }
        if (($codeValue -eq '' -or $codeValue -match '^NU\d+$') -and $line -match '(?i)Unable to load the service index|No such host|Name or service not known|connection (refused|reset)') { return [pscustomobject]@{ Kind = 'networkOrFeedUnavailable'; Status = 'environmentBlocked' } }
    }
    return [pscustomobject]@{ Kind = 'compilationOrBuildFailure'; Status = 'failed' }
}

# --- Guarda da D15 (seção 4.3) ---

function Get-B129IfDirectiveLines {
    param([string[]]$Lines)
    $found = [System.Collections.Generic.List[object]]::new()
    for ($index = 0; $index -lt $Lines.Count; $index++) {
        if ($Lines[$index] -cmatch '^\s*#\s*if\b') { $found.Add([ordered]@{ line = $index + 1; text = $Lines[$index].Trim() }) }
    }
    return @($found)
}
