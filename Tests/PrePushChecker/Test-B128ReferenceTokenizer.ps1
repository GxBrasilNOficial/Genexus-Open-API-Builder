#requires -Version 7.4
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '..\..\scripts\B128-ReferenceTokenizer.ps1')
$script:assertions = 0

function Assert-B128 {
    param([bool]$Condition, [string]$Name)
    $script:assertions++
    if (-not $Condition) { throw "FAIL $Name" }
}

function Invoke-B128TestGit {
    param([string]$Repository, [string[]]$Arguments)
    $result = Invoke-B128GitBytes -Repository $Repository -Arguments $Arguments
    if ($result.ExitCode -ne 0) {
        $errorText = [System.Text.UTF8Encoding]::new($false, $true).GetString($result.Bytes)
        throw "git $($Arguments -join ' ') falhou (exit $($result.ExitCode)): $($result.Stderr) $errorText"
    }
    return [System.Text.UTF8Encoding]::new($false, $true).GetString($result.Bytes).TrimEnd("`r", "`n")
}

$formatted = @(Get-B128Candidates '**Api.cs**:100, `Api.cs`:100, Api.cs\:100, Api.cs:L100, Api.cs:#100, Api.cs:100.')
Assert-B128 ($formatted.Count -eq 6) 'formatting and invalid numeric prefixes are candidates'
Assert-B128 (@($formatted | Where-Object { $_.canonical -ceq 'Api.cs:100' }).Count -eq 4) 'Markdown and escaped forms share a mobile key'
Assert-B128 (@($formatted | Where-Object { $_.canonical -ceq 'Api.cs:L100' -and -not $_.validSyntax }).Count -eq 1) 'L numeric prefix is invalid'
Assert-B128 (@($formatted | Where-Object { $_.canonical -ceq 'Api.cs:#100' -and -not $_.validSyntax }).Count -eq 1) '# numeric prefix is invalid'
Assert-B128 (@(Get-B128Candidates 'Api.cs:')[0].validSyntax -eq $false) 'empty mobile location is an invalid candidate'

$malformed = @(Get-B128Candidates 'Api.cs:100/999 Api.cs:100,105 Api.cs:100;105 Api.cs:100+105 Api.cs:100‑105 Api.cs:100−105 Api.cs:100–105 Api.cs:100abc Api.cs:100$105 SHA:path.cs#L2#L999')
Assert-B128 ($malformed.Count -eq 10 -and @($malformed | Where-Object validSyntax).Count -eq 0) 'malformed continuations consume the entire token'
$gitDiffFailure = @(Get-B128ChangelogWarningsFromDiff -ExitCode 2 -Bytes ([byte[]]@()))
Assert-B128 ($gitDiffFailure.Count -eq 1 -and $gitDiffFailure[0] -match '^b128-changelog-warning-incomplete:' -and $gitDiffFailure[0] -match 'exit 2') 'CHANGELOG warning reports Git diff failure explicitly'
$undecodableDiff = @(Get-B128ChangelogWarningsFromDiff -ExitCode 0 -Bytes ([byte[]]@(0xFF)))
Assert-B128 ($undecodableDiff.Count -eq 1 -and $undecodableDiff[0] -match '^b128-changelog-warning-incomplete:') 'CHANGELOG warning reports undecodable diff explicitly'
Assert-B128 (@(Get-B128Candidates 'ExtensionLanguage.cs`: enum and Api.cs:line 100').Count -eq 0) 'enum names and explicit out of scope prose are ignored'
$fencedMarkdown = '```text' + [Environment]::NewLine + 'Api.cs:100' + [Environment]::NewLine + '```'
Assert-B128 (@(Get-B128Candidates $fencedMarkdown).Count -eq 1) 'inline and fenced source text are scanned'

$sha = 'a' * 40
$fixed = @(Get-B128Candidates "**${sha}:Src/Domain/Api.cs#L2**")
Assert-B128 ($fixed.Count -eq 1 -and $fixed[0].canonical -ceq "${sha}:Src/Domain/Api.cs#L2" -and $fixed[0].validSyntax) 'emphasis is excluded from fixed address key'
$fixedUnderscoreEmphasis = @(Get-B128Candidates "__${sha}:Src/Domain/Api.cs#L2__")
Assert-B128 ($fixedUnderscoreEmphasis.Count -eq 1 -and $fixedUnderscoreEmphasis[0].canonical -ceq "${sha}:Src/Domain/Api.cs#L2" -and $fixedUnderscoreEmphasis[0].validSyntax) 'underscore emphasis is excluded from fixed address key'
$fixedUrl = @(Get-B128Candidates "https://example.invalid/blob/${sha}:Src/Domain/Api.cs#L2")
Assert-B128 ($fixedUrl.Count -eq 1 -and -not $fixedUrl[0].validSyntax -and $fixedUrl[0].canonical -ne "${sha}:Src/Domain/Api.cs#L2") 'URL prefix cannot collapse into a valid fixed citation key'
$fixedUnitRange = @(Get-B128Candidates "${sha}:Src/Domain/Api.cs#L100-L100")
Assert-B128 ($fixedUnitRange.Count -eq 1 -and $fixedUnitRange[0].validSyntax) 'unit fixed range is valid syntax'
Assert-B128 (-not @(Get-B128Candidates "${sha}:../Src/Domain/Api.cs#L2")[0].validSyntax) 'parent path is invalid'
Assert-B128 (-not @(Get-B128Candidates "${sha}:/Src/Domain/Api.cs#L2")[0].validSyntax) 'absolute path is invalid'

$base = @(Get-B128Candidates 'Api.cs:100')
$head = @(Get-B128Candidates 'Api.cs:100,105')
$excess = @(Get-B128ExcessCandidates -BaseCandidates $base -HeadCandidates $head)
Assert-B128 ($excess.Count -eq 1 -and -not $excess[0].validSyntax) 'malformed extension cannot hide behind an existing prefix'
$fixedSlash = @(Get-B128Candidates "${sha}:Src/Domain/Api.cs#L2")
$fixedBackslash = @(Get-B128Candidates "${sha}:Src\Domain\Api.cs#L2")
Assert-B128 (@(Get-B128ExcessCandidates -BaseCandidates $fixedSlash -HeadCandidates $fixedBackslash).Count -eq 1) 'fixed slash mutation creates an excess token'
Assert-B128 (@(Get-B128ExcessCandidates -BaseCandidates @() -HeadCandidates $fixedSlash)[0].validSyntax) 'a new syntactically valid fixed citation is not rejected by tokenization'

$terminal = @(Get-B128Candidates 'Api.cs:100. Api.cs:100, and Api.cs:100; prose')
Assert-B128 ($terminal.Count -eq 3 -and @($terminal | Where-Object validSyntax).Count -eq 3) 'terminal punctuation is separated from citation tokens'
$physical = [System.Text.Encoding]::UTF8.GetBytes("a`r`nb`nc`rd")
Assert-B128 ((Get-B128PhysicalLineCount $physical) -eq 4) 'physical line count handles CRLF, LF, CR, and no final newline'
$lineMixed = @(Get-B128Candidates "Api.cs:1`r`nApi.cs:2`nApi.cs:3`rApi.cs:4")
Assert-B128 (@($lineMixed | ForEach-Object line) -join ',' -ceq '1,2,3,4') 'candidate source positions count CRLF, LF, and CR once each'

$fixture = Join-Path ([System.IO.Path]::GetTempPath()) ("B128-GitProof-" + [guid]::NewGuid().ToString('N'))
[void][System.IO.Directory]::CreateDirectory($fixture)
try {
    [void](Invoke-B128TestGit $fixture @('init', '-b', 'main'))
    [void](Invoke-B128TestGit $fixture @('config', 'user.name', 'B128 Fixture'))
    [void](Invoke-B128TestGit $fixture @('config', 'user.email', 'b128@example.invalid'))
    $sourceDirectory = Join-Path $fixture 'Src\Domain'
    [void][System.IO.Directory]::CreateDirectory($sourceDirectory)
    $sourcePath = Join-Path $sourceDirectory 'ação fonte.cs'
    [System.IO.File]::WriteAllBytes($sourcePath, [System.Text.Encoding]::UTF8.GetBytes("class Api {`r`n // traço – Unicode`r`n}`r`n"))
    $fakeSourceDirectory = Join-Path $sourceDirectory 'Fake.cs'
    [void][System.IO.Directory]::CreateDirectory($fakeSourceDirectory)
    [System.IO.File]::WriteAllText((Join-Path $fakeSourceDirectory 'child.txt'), 'not C#', [System.Text.UTF8Encoding]::new($false))
    [void](Invoke-B128TestGit $fixture @('add', '-A'))
    [void](Invoke-B128TestGit $fixture @('commit', '-m', 'source base'))
    $baseSha = Invoke-B128TestGit $fixture @('rev-parse', 'HEAD')

    $invalidUtf8Path = Join-Path $sourceDirectory 'invalid-utf8.cs'
    [System.IO.File]::WriteAllBytes($invalidUtf8Path, [byte[]]@(0xFF, 0x0A))
    [void](Invoke-B128TestGit $fixture @('add', 'Src/Domain/invalid-utf8.cs'))
    [void](Invoke-B128TestGit $fixture @('commit', '-m', 'invalid UTF-8 source'))
    $invalidUtf8Sha = Invoke-B128TestGit $fixture @('rev-parse', 'HEAD')
    $invalidUtf8Candidate = (Get-B128Candidates "${invalidUtf8Sha}:Src/Domain/invalid-utf8.cs#L1")[0]
    $invalidUtf8Result = Test-B128FixedReference -RepositoryRoot $fixture -Candidate $invalidUtf8Candidate
    Assert-B128 (-not $invalidUtf8Result.valid -and $invalidUtf8Result.environmentBlocked -and $invalidUtf8Result.reason -match 'UTF-8') 'undecodable source blob is environmentBlocked'

    $emptyRepository = Join-Path ([System.IO.Path]::GetTempPath()) ("B128-NotRepo-" + [guid]::NewGuid().ToString('N'))
    try {
        [void][System.IO.Directory]::CreateDirectory($emptyRepository)
        $gitFailureCandidate = (Get-B128Candidates "${baseSha}:Src/Domain/ação fonte.cs#L1")[0]
        $gitFailureResult = Test-B128FixedReference -RepositoryRoot $emptyRepository -Candidate $gitFailureCandidate
    }
    finally {
        if (Test-Path -LiteralPath $emptyRepository) { Remove-Item -LiteralPath $emptyRepository -Recurse -Force }
    }
    Assert-B128 (-not $gitFailureResult.valid -and $gitFailureResult.environmentBlocked) 'Git operational errors are environmentBlocked'

    $validFixedText = "${baseSha}:Src/Domain/ação fonte.cs#L2-L3"
    $validFixed = (Get-B128Candidates $validFixedText)[0]
    $validResult = Test-B128FixedReference -RepositoryRoot $fixture -Candidate $validFixed
    Assert-B128 ($validResult.valid -and -not $validResult.environmentBlocked) "ancestral commit, Unicode path, CRLF blob, and in-range lines pass: $($validResult | ConvertTo-Json -Compress)"
    $validUnitRange = (Get-B128Candidates "${baseSha}:Src/Domain/ação fonte.cs#L2-L2")[0]
    $validUnitRangeResult = Test-B128FixedReference -RepositoryRoot $fixture -Candidate $validUnitRange
    Assert-B128 ($validUnitRangeResult.valid -and -not $validUnitRangeResult.environmentBlocked) 'in-range unit citation range passes Git validation'

    $hugeLine = (Get-B128Candidates ("${baseSha}:Src/Domain/ação fonte.cs#L999999999999999999999999999999999999"))[0]
    $hugeResult = Test-B128FixedReference -RepositoryRoot $fixture -Candidate $hugeLine
    Assert-B128 (-not $hugeResult.valid -and -not $hugeResult.environmentBlocked -and $hugeResult.reason -match 'linha fora') 'large line number fails without Int32 overflow'

    $missingPath = (Get-B128Candidates "${baseSha}:Src/Domain/Missing.cs#L1")[0]
    $missingPathResult = Test-B128FixedReference -RepositoryRoot $fixture -Candidate $missingPath
    Assert-B128 (-not $missingPathResult.valid -and $missingPathResult.reason -match 'path não existe') 'missing source path is an invalid citation'
    $directoryPath = (Get-B128Candidates "${baseSha}:Src/Domain/Fake.cs#L1")[0]
    $directoryResult = Test-B128FixedReference -RepositoryRoot $fixture -Candidate $directoryPath
    Assert-B128 (-not $directoryResult.valid -and $directoryResult.reason -match 'não é blob C#') 'directory named .cs is not a source blob'
    $missingSha = (Get-B128Candidates ("$('0' * 40):Src/Domain/ação fonte.cs#L1"))[0]
    $missingShaResult = Test-B128FixedReference -RepositoryRoot $fixture -Candidate $missingSha
    Assert-B128 (-not $missingShaResult.valid -and $missingShaResult.reason -match 'SHA inexistente') 'unknown commit SHA is an invalid citation'

    $blobOid = Invoke-B128TestGit $fixture @('rev-parse', "${baseSha}:Src/Domain/ação fonte.cs")
    $blobAsSha = (Get-B128Candidates ("${blobOid}:Src/Domain/ação fonte.cs#L1"))[0]
    $blobAsShaResult = Test-B128FixedReference -RepositoryRoot $fixture -Candidate $blobAsSha
    Assert-B128 (-not $blobAsShaResult.valid -and $blobAsShaResult.reason -match 'não designa um commit') 'blob object ID is not a commit SHA'

    [void](Invoke-B128TestGit $fixture @('checkout', '-b', 'lateral'))
    [System.IO.File]::WriteAllText((Join-Path $fixture 'lateral.txt'), 'lateral', [System.Text.UTF8Encoding]::new($false))
    [void](Invoke-B128TestGit $fixture @('add', 'lateral.txt'))
    [void](Invoke-B128TestGit $fixture @('commit', '-m', 'lateral commit'))
    $lateralSha = Invoke-B128TestGit $fixture @('rev-parse', 'HEAD')
    [void](Invoke-B128TestGit $fixture @('checkout', 'main'))
    $lateral = (Get-B128Candidates ("${lateralSha}:Src/Domain/ação fonte.cs#L1"))[0]
    $lateralResult = Test-B128FixedReference -RepositoryRoot $fixture -Candidate $lateral
    Assert-B128 (-not $lateralResult.valid -and -not $lateralResult.environmentBlocked -and $lateralResult.reason -match 'commit lateral') 'lateral commit is an invalid citation'

    [void][System.IO.Directory]::CreateDirectory((Join-Path $fixture 'Docs'))
    [System.IO.File]::WriteAllText((Join-Path $fixture 'Docs\ação com espaço.md'), $validFixedText + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
    [void](Invoke-B128TestGit $fixture @('add', 'Docs/ação com espaço.md'))
    [void](Invoke-B128TestGit $fixture @('commit', '-m', 'add documentation citation'))
    $treeResult = Get-B128TreeCandidates -RepositoryRoot $fixture -Reference 'HEAD'
    Assert-B128 ($treeResult.Documents -eq 1 -and $treeResult.Candidates.Count -eq 1 -and $treeResult.Candidates[0].path -ceq 'Docs/ação com espaço.md') 'NUL Git path prefilter preserves accents and spaces'

    $shallowPath = Join-Path ([System.IO.Path]::GetTempPath()) ("B128-Shallow-" + [guid]::NewGuid().ToString('N'))
    try {
        $fileUrl = 'file:///' + $fixture.Replace('\', '/')
        [void](Invoke-B128TestGit $fixture @('clone', '--depth', '1', '--branch', 'main', $fileUrl, $shallowPath))
        $shallowCandidate = (Get-B128Candidates ("${baseSha}:Src/Domain/ação fonte.cs#L1"))[0]
        $shallowResult = Test-B128FixedReference -RepositoryRoot $shallowPath -Candidate $shallowCandidate
        Assert-B128 (-not $shallowResult.valid -and $shallowResult.environmentBlocked) 'unavailable historical commit in shallow clone is environmentBlocked'
    }
    finally {
        if (Test-Path -LiteralPath $shallowPath) { Remove-Item -LiteralPath $shallowPath -Recurse -Force }
    }

    $noCandidates = Get-B128TreeCandidates -RepositoryRoot $fixture -Reference $baseSha
    Assert-B128 ($noCandidates.Candidates.Count -eq 0 -and $noCandidates.Documents -eq 0) 'no matching Docs blobs is an empty success set'
}
finally {
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
}

Write-Output "B128_REFERENCE_TOKENIZER_OK assertions=$script:assertions"
