# Shared tokenizer for the B128 checker and its regression tests.
Set-StrictMode -Version Latest

$script:B128MobileMarker = [regex]::new('(?i)\.cs(?<bridge>(?:\*\*|__|[*_`]|\\)*)\:')
$script:B128FixedMarker = [regex]::new('(?i)\.cs#L')
$script:B128FixedShape = [regex]::new('^(?<sha>[0-9a-f]{40}):(?<path>[^:<>\\]+\.cs)#L(?<first>[1-9][0-9]*)(?:-L(?<last>[1-9][0-9]*))?$', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
$script:B128MobileShape = [regex]::new('^[1-9][0-9]*(?:-[1-9][0-9]*)?$')

function Test-B128PathCharacter {
    param([char]$Character)
    return [char]::IsLetterOrDigit($Character) -or $Character -in @([char]'_', [char]'.', [char]'/', [char]'\', [char]'-', [char]':')
}

function Get-B128LocationEnd {
    param([string]$Text, [int]$Start)
    $index = $Start
    while ($index -lt $Text.Length) {
        $character = $Text[$index]
        if ([char]::IsWhiteSpace($character) -or $character -in @([char]')', [char]']', [char]'}', [char]'>', [char]'*', [char]'_', [char]'`')) { break }
        if ($character -in @([char]'.', [char]',', [char]';', [char]'+') -and ($index + 1 -ge $Text.Length -or [char]::IsWhiteSpace($Text[$index + 1]) -or $Text[$index + 1] -in @([char]')', [char]']', [char]'}', [char]'>', [char]'*', [char]'_', [char]'`'))) { break }
        $index++
    }
    return $index
}

function Get-B128CandidateLine {
    param([string]$Text, [int]$Offset)
    $line = 1
    for ($index = 0; $index -lt $Offset; $index++) {
        if ($Text[$index] -eq "`r") { $line++ }
        elseif ($Text[$index] -eq "`n" -and ($index -eq 0 -or $Text[$index - 1] -ne "`r")) { $line++ }
    }
    return $line
}

function Get-B128Candidates {
    param([AllowNull()][string]$Text)
    if ($null -eq $Text) { return @() }
    $items = [System.Collections.Generic.List[object]]::new()
    foreach ($marker in $script:B128MobileMarker.Matches($Text)) {
        $locationStart = $marker.Index + $marker.Length
        $locationPrefix = ''
        $missingLocation = $false
        if ($locationStart -lt $Text.Length -and $Text[$locationStart] -in @([char]'L', [char]'l', [char]'#') -and $locationStart + 1 -lt $Text.Length -and [char]::IsDigit($Text[$locationStart + 1])) {
            $locationPrefix = [string]$Text[$locationStart]
            $locationStart++
        }
        elseif ($locationStart -ge $Text.Length -or $Text[$locationStart] -in @([char]')', [char]']', [char]'}', [char]'>', [char]'*', [char]'_', [char]'`')) {
            $missingLocation = $true
        }
        elseif (-not [char]::IsDigit($Text[$locationStart])) { continue }

        $tokenStart = $marker.Index
        while ($tokenStart -gt 0 -and (Test-B128PathCharacter $Text[$tokenStart - 1])) { $tokenStart-- }
        $path = $Text.Substring($tokenStart, $marker.Index - $tokenStart)
        if ([string]::IsNullOrEmpty($path)) { continue }
        $locationEnd = if ($missingLocation) { $locationStart } else { Get-B128LocationEnd $Text $locationStart }
        $location = $Text.Substring($locationStart, $locationEnd - $locationStart)
        $raw = "$path.cs:$locationPrefix$location"
        $completeLocation = "$locationPrefix$location"
        $valid = $script:B128MobileShape.IsMatch($completeLocation)
        if ($valid -and $completeLocation.Contains('-')) {
            $range = @($completeLocation -split '-')
            $valid = [System.Numerics.BigInteger]::Parse($range[1]) -ge [System.Numerics.BigInteger]::Parse($range[0])
        }
        $items.Add([pscustomobject]@{ kind = 'mobile'; raw = $raw; canonical = $raw.Replace('\', '/'); validSyntax = $valid; line = (Get-B128CandidateLine $Text $tokenStart) })
    }

    foreach ($marker in $script:B128FixedMarker.Matches($Text)) {
        $tokenStart = $marker.Index
        $priorText = $Text.Substring(0, $marker.Index)
        $shaPrefixes = [regex]::Matches($priorText, '(?i)[0-9a-f]{40}:')
        if ($shaPrefixes.Count -gt 0) {
            $tokenStart = $shaPrefixes[$shaPrefixes.Count - 1].Index
            while ($tokenStart -gt 0 -and (Test-B128PathCharacter $Text[$tokenStart - 1])) { $tokenStart-- }
            $prefixBeforeSha = $Text.Substring($tokenStart, $shaPrefixes[$shaPrefixes.Count - 1].Index - $tokenStart)
            if ($prefixBeforeSha -match '^(?:\*\*|__|[*_`])$') { $tokenStart = $shaPrefixes[$shaPrefixes.Count - 1].Index }
        }
        else { while ($tokenStart -gt 0 -and (Test-B128PathCharacter $Text[$tokenStart - 1])) { $tokenStart-- } }
        $prefix = $Text.Substring($tokenStart, $marker.Index - $tokenStart)
        if ([string]::IsNullOrEmpty($prefix)) { continue }
        $locationStart = $marker.Index + $marker.Length
        $locationEnd = Get-B128LocationEnd $Text $locationStart
        $location = $Text.Substring($locationStart, $locationEnd - $locationStart)
        $raw = "$prefix.cs#L$location"
        $valid = $script:B128FixedShape.IsMatch($raw)
        if ($valid) {
            $match = $script:B128FixedShape.Match($raw)
            $path = $match.Groups['path'].Value
            $valid = -not [string]::IsNullOrWhiteSpace($path) -and -not $path.StartsWith('/') -and -not $path.StartsWith('./') -and -not $path.Contains('\') -and $path -notmatch '(^|/)\.\.(/|$)' -and $path -notmatch '^https?:'
            if ($valid -and $match.Groups['last'].Success) { $valid = [System.Numerics.BigInteger]::Parse($match.Groups['last'].Value) -ge [System.Numerics.BigInteger]::Parse($match.Groups['first'].Value) }
        }
        $items.Add([pscustomobject]@{ kind = 'fixed'; raw = $raw; canonical = $raw; validSyntax = $valid; line = (Get-B128CandidateLine $Text $tokenStart) })
    }
    return @($items.ToArray())
}

function Get-B128ExcessCandidates {
    param([object[]]$BaseCandidates, [object[]]$HeadCandidates)
    $baseCounts = [System.Collections.Generic.Dictionary[string,int]]::new([System.StringComparer]::Ordinal)
    foreach ($item in $BaseCandidates) {
        if ($baseCounts.ContainsKey($item.canonical)) { $baseCounts[$item.canonical]++ } else { $baseCounts[$item.canonical] = 1 }
    }
    $seen = [System.Collections.Generic.Dictionary[string,int]]::new([System.StringComparer]::Ordinal)
    $extra = [System.Collections.Generic.List[object]]::new()
    foreach ($item in $HeadCandidates) {
        if ($seen.ContainsKey($item.canonical)) { $seen[$item.canonical]++ } else { $seen[$item.canonical] = 1 }
        $budget = if ($baseCounts.ContainsKey($item.canonical)) { $baseCounts[$item.canonical] } else { 0 }
        if ($seen[$item.canonical] -gt $budget) { $extra.Add($item) }
    }
    return @($extra.ToArray())
}

function Invoke-B128GitBytes {
    param([string]$Repository, [string[]]$Arguments)
    $info = [System.Diagnostics.ProcessStartInfo]::new()
    $info.FileName = 'git'
    $info.WorkingDirectory = $Repository
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    foreach ($argument in $Arguments) { [void]$info.ArgumentList.Add($argument) }
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $info
    [void]$process.Start()
    $memory = [System.IO.MemoryStream]::new()
    $stdoutTask = $process.StandardOutput.BaseStream.CopyToAsync($memory)
    $stderrTask = $process.StandardError.ReadToEndAsync()
    [System.Threading.Tasks.Task]::WaitAll(@($stdoutTask, $stderrTask))
    $process.WaitForExit()
    return [pscustomobject]@{ ExitCode = $process.ExitCode; Bytes = $memory.ToArray(); Stderr = $stderrTask.Result }
}

function ConvertFrom-B128NulUtf8 {
    param([byte[]]$Bytes)
    $values = [System.Collections.Generic.List[string]]::new()
    $start = 0
    for ($index = 0; $index -le $Bytes.Length; $index++) {
        if ($index -eq $Bytes.Length -or $Bytes[$index] -eq 0) {
            $length = $index - $start
            if ($length -gt 0) {
                $segment = [byte[]]::new($length)
                [Array]::Copy($Bytes, $start, $segment, 0, $length)
                $values.Add([System.Text.UTF8Encoding]::new($false, $true).GetString($segment))
            }
            $start = $index + 1
        }
    }
    return @($values.ToArray())
}

function Get-B128PhysicalLineCount {
    param([byte[]]$Bytes)
    if ($Bytes.Length -eq 0) { return [System.Numerics.BigInteger]::Zero }
    [long]$lineEnds = 0
    for ($index = 0; $index -lt $Bytes.Length; $index++) {
        if ($Bytes[$index] -eq 10) { $lineEnds++ }
        elseif ($Bytes[$index] -eq 13 -and ($index + 1 -ge $Bytes.Length -or $Bytes[$index + 1] -ne 10)) { $lineEnds++ }
    }
    if ($Bytes[-1] -notin @(10, 13)) { $lineEnds++ }
    return [System.Numerics.BigInteger]$lineEnds
}

function Get-B128TreeCandidates {
    param([string]$RepositoryRoot, [string]$Reference)
    $grep = Invoke-B128GitBytes -Repository $RepositoryRoot -Arguments @('grep', '--fixed-strings', '--ignore-case', '--files-with-matches', '-z', '-e', '.cs', $Reference, '--', 'Docs')
    if ($grep.ExitCode -eq 1) { return [pscustomobject]@{ Candidates = @(); Documents = 0 } }
    if ($grep.ExitCode -ne 0) { throw "git grep falhou em '$Reference' (exit $($grep.ExitCode)): $($grep.Stderr)" }

    $paths = @(
        ConvertFrom-B128NulUtf8 $grep.Bytes |
            ForEach-Object { if ($_.StartsWith("$Reference`:", [System.StringComparison]::Ordinal)) { $_.Substring($Reference.Length + 1) } else { $_ } } |
            Where-Object { $_ -match '(?i)\.md$' }
    )
    $all = [System.Collections.Generic.List[object]]::new()
    foreach ($path in $paths) {
        $blob = Invoke-B128GitBytes -Repository $RepositoryRoot -Arguments @('cat-file', 'blob', "$Reference`:$path")
        if ($blob.ExitCode -ne 0) { throw "git cat-file falhou para '$Reference`:$path': $($blob.Stderr)" }
        $text = [System.Text.UTF8Encoding]::new($false, $true).GetString($blob.Bytes)
        foreach ($candidate in @(Get-B128Candidates $text)) {
            $all.Add([pscustomobject]@{ kind = $candidate.kind; raw = $candidate.raw; canonical = $candidate.canonical; validSyntax = $candidate.validSyntax; line = $candidate.line; path = $path })
        }
    }
    return [pscustomobject]@{ Candidates = @($all.ToArray()); Documents = $paths.Count }
}

function Get-B128ChangelogWarningsFromDiff {
    param([int]$ExitCode, [AllowNull()][byte[]]$Bytes)
    $warnings = [System.Collections.Generic.List[string]]::new()
    if ($ExitCode -ne 0) {
        $warnings.Add("b128-changelog-warning-incomplete: não foi possível ler o diff commitado de Docs/ (exit $ExitCode); avisos de linha do CHANGELOG não foram avaliados.")
        return @($warnings.ToArray())
    }
    try { $diffText = [System.Text.UTF8Encoding]::new($false, $true).GetString($Bytes) }
    catch {
        $warnings.Add('b128-changelog-warning-incomplete: o diff commitado de Docs/ não pôde ser decodificado; avisos de linha do CHANGELOG não foram avaliados.')
        return @($warnings.ToArray())
    }

    $lineWord = '(?i)\b(?:linhas?|lines?|l[ií]neas?)\s+[0-9]+(?:\s*[-–]\s*[0-9]+)?'
    $priorLine = ''
    foreach ($raw in @($diffText -split "`n")) {
        $line = $raw.TrimEnd("`r")
        if ($line -match '^@@') { $priorLine = ''; continue }
        if ($line.StartsWith('+++') -or $line.StartsWith('---')) { $priorLine = ''; continue }
        if ($line.StartsWith('+')) {
            $added = $line.Substring(1)
            if ([regex]::IsMatch($added, $lineWord) -and (($added -match '(?i)CHANGELOG') -or ($priorLine -match '(?i)CHANGELOG'))) {
                $warnings.Add('b128-changelog-line-reference: linha adicionada em Docs/ combina referência numérica provável ao CHANGELOG; indique versão/seção/entrada publicada ou a frente e seu documento de evidência para [Unreleased]. Aviso heurístico, não bloqueante.')
            }
            $priorLine = $added
            continue
        }
        if ($line.StartsWith('-')) { continue }
        if ($line.StartsWith(' ')) { $priorLine = $line.Substring(1); continue }
        $priorLine = ''
    }
    return @($warnings.ToArray())
}

function Test-B128FixedReference {
    param([string]$RepositoryRoot, [object]$Candidate)
    $shape = $script:B128FixedShape.Match($Candidate.raw)
    if (-not $Candidate.validSyntax -or -not $shape.Success) { return [pscustomobject]@{ valid = $false; environmentBlocked = $false; reason = 'formato inválido' } }
    $sha = $shape.Groups['sha'].Value
    $sourcePath = $shape.Groups['path'].Value
    $commitType = Invoke-B128GitBytes -Repository $RepositoryRoot -Arguments @('cat-file', '-t', $sha)
    if ($commitType.ExitCode -ne 0) {
        $shallow = Invoke-B128GitBytes -Repository $RepositoryRoot -Arguments @('rev-parse', '--is-shallow-repository')
        if ($shallow.ExitCode -ne 0) { return [pscustomobject]@{ valid = $false; environmentBlocked = $true; reason = 'não foi possível determinar se o clone é raso' } }
        $isShallow = [System.Text.UTF8Encoding]::new($false, $true).GetString($shallow.Bytes).Trim() -ceq 'true'
        if ($isShallow) { return [pscustomobject]@{ valid = $false; environmentBlocked = $true; reason = 'commit citado indisponível em clone raso' } }
        return [pscustomobject]@{ valid = $false; environmentBlocked = $false; reason = 'SHA inexistente ou inacessível como commit' }
    }
    $typeText = [System.Text.UTF8Encoding]::new($false, $true).GetString($commitType.Bytes).Trim()
    if ($typeText -cne 'commit') { return [pscustomobject]@{ valid = $false; environmentBlocked = $false; reason = 'SHA não designa um commit' } }

    $ancestor = Invoke-B128GitBytes -Repository $RepositoryRoot -Arguments @('merge-base', '--is-ancestor', $sha, 'HEAD')
    if ($ancestor.ExitCode -eq 1) {
        $shallow = Invoke-B128GitBytes -Repository $RepositoryRoot -Arguments @('rev-parse', '--is-shallow-repository')
        if ($shallow.ExitCode -ne 0) { return [pscustomobject]@{ valid = $false; environmentBlocked = $true; reason = 'não foi possível determinar se o clone é raso' } }
        $isShallow = [System.Text.UTF8Encoding]::new($false, $true).GetString($shallow.Bytes).Trim() -ceq 'true'
        if ($isShallow) { return [pscustomobject]@{ valid = $false; environmentBlocked = $true; reason = 'ancestralidade indeterminável em clone raso' } }
        return [pscustomobject]@{ valid = $false; environmentBlocked = $false; reason = 'commit lateral, não ancestral de HEAD' }
    }
    if ($ancestor.ExitCode -ne 0) { return [pscustomobject]@{ valid = $false; environmentBlocked = $true; reason = "git merge-base falhou (exit $($ancestor.ExitCode))" } }

    $treeEntry = Invoke-B128GitBytes -Repository $RepositoryRoot -Arguments @('ls-tree', '-z', '--full-tree', $sha, '--', ":(literal)$sourcePath")
    if ($treeEntry.ExitCode -ne 0) { return [pscustomobject]@{ valid = $false; environmentBlocked = $true; reason = "git ls-tree falhou (exit $($treeEntry.ExitCode))" } }
    $records = @(ConvertFrom-B128NulUtf8 $treeEntry.Bytes)
    if ($records.Count -eq 0) { return [pscustomobject]@{ valid = $false; environmentBlocked = $false; reason = 'path não existe no commit citado' } }
    $record = $records[0]
    $tab = $record.IndexOf([char]9)
    if ($tab -lt 0) { return [pscustomobject]@{ valid = $false; environmentBlocked = $true; reason = 'saída de git ls-tree não pôde ser interpretada' } }
    $header = $record.Substring(0, $tab)
    if ($header -notmatch '^\d+ blob (?<oid>[0-9a-f]+)$') { return [pscustomobject]@{ valid = $false; environmentBlocked = $false; reason = 'path não é blob C#' } }
    $blob = Invoke-B128GitBytes -Repository $RepositoryRoot -Arguments @('cat-file', 'blob', $Matches['oid'])
    if ($blob.ExitCode -ne 0) { return [pscustomobject]@{ valid = $false; environmentBlocked = $true; reason = "git cat-file do blob falhou (exit $($blob.ExitCode))" } }
    try { [void][System.Text.UTF8Encoding]::new($false, $true).GetString($blob.Bytes) }
    catch { return [pscustomobject]@{ valid = $false; environmentBlocked = $true; reason = 'bytes do blob não são UTF-8 confiável' } }

    $lineCount = Get-B128PhysicalLineCount $blob.Bytes
    $first = [System.Numerics.BigInteger]::Parse($shape.Groups['first'].Value)
    $last = if ($shape.Groups['last'].Success) { [System.Numerics.BigInteger]::Parse($shape.Groups['last'].Value) } else { $first }
    if ($first -gt $lineCount -or $last -gt $lineCount) { return [pscustomobject]@{ valid = $false; environmentBlocked = $false; reason = "linha fora do blob ($lineCount linhas físicas)" } }
    return [pscustomobject]@{ valid = $true; environmentBlocked = $false; reason = 'commit ancestral, blob C# e faixa existentes' }
}
