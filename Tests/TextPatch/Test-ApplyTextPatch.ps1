#requires -Version 7.4
<#
.SYNOPSIS
    Testes offline de scripts/Apply-TextPatch.ps1 (B122).

.NOTES
    Este harness só sai com exit 0 (ok) ou 1 (falha da suíte).
    Nunca propaga o exit code do script sob teste (exit 2 = INVALID_INPUT no script,
    mas environmentBlocked no checker pré-push).
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$scriptPath = Join-Path $repositoryRoot 'scripts\Apply-TextPatch.ps1'
$utf8 = [Text.UTF8Encoding]::new($false)
$fixtureLeaf = 'b122-textpatch-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$fixtureRoot = Join-Path $repositoryRoot (Join-Path 'Temp' $fixtureLeaf)

function Get-Rel {
    param([string]$FileName)
    return ('Temp/' + $fixtureLeaf + '/' + $FileName)
}

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw "ASSERT_FAILED: $Message" }
}

function Invoke-ApplyTextPatch {
    param(
        [string]$ManifestPath,
        [switch]$WhatIf
    )

    $args = @('-NoProfile', '-File', $scriptPath, '-ManifestPath', $ManifestPath)
    if ($WhatIf) { $args += '-WhatIf' }

    $info = [System.Diagnostics.ProcessStartInfo]::new()
    $info.FileName = 'pwsh'
    $info.WorkingDirectory = $repositoryRoot
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    foreach ($a in $args) { [void]$info.ArgumentList.Add($a) }
    $p = [System.Diagnostics.Process]::new()
    $p.StartInfo = $info
    [void]$p.Start()
    $stdoutTask = $p.StandardOutput.ReadToEndAsync()
    $stderrTask = $p.StandardError.ReadToEndAsync()
    $p.WaitForExit()
    [System.Threading.Tasks.Task]::WaitAll(@($stdoutTask, $stderrTask))
    $stdout = $stdoutTask.Result
    $stderr = $stderrTask.Result
    $jsonLine = ($stdout -split "`r?`n" | Where-Object { $_.Trim().StartsWith('{') } | Select-Object -Last 1)
    $result = $null
    if (-not [string]::IsNullOrWhiteSpace($jsonLine)) {
        $result = $jsonLine | ConvertFrom-Json
    }
    return [pscustomobject]@{
        ExitCode = $p.ExitCode
        StdOut   = $stdout
        StdErr   = $stderr
        Result   = $result
    }
}

function New-Manifest {
    param(
        [string]$RelativePath,
        [object[]]$Ops,
        [string]$FileName = 'manifest.json'
    )
    $obj = [ordered]@{
        schemaVersion = 1
        path          = $RelativePath.Replace('\', '/')
        ops           = @($Ops)
    }
    $path = Join-Path $fixtureRoot $FileName
    [IO.File]::WriteAllText($path, (($obj | ConvertTo-Json -Depth 6) + "`n"), $utf8)
    return $path
}

function Get-FileSha {
    param([string]$Path)
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-EolKindBytes {
    param([byte[]]$Bytes)
    $crlf = 0; $lf = 0; $cr = 0
    for ($i = 0; $i -lt $Bytes.Length; $i++) {
        if ($Bytes[$i] -eq 10) {
            if ($i -gt 0 -and $Bytes[$i - 1] -eq 13) { $crlf++ } else { $lf++ }
        }
        elseif ($Bytes[$i] -eq 13 -and ($i + 1 -ge $Bytes.Length -or $Bytes[$i + 1] -ne 10)) { $cr++ }
    }
    if ($crlf -gt 0 -and $lf -eq 0 -and $cr -eq 0) { return 'CRLF' }
    if ($lf -gt 0 -and $crlf -eq 0 -and $cr -eq 0) { return 'LF' }
    if ($crlf -eq 0 -and $lf -eq 0 -and $cr -eq 0) { return 'NONE' }
    return 'MIXED'
}

try {
    Assert-True (Test-Path -LiteralPath $scriptPath -PathType Leaf) "Script ausente: $scriptPath"
    New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null

    # --- A WhatIf unique ---
    $relA = Get-Rel 'ok_lf.txt'
    $absA = Join-Path $repositoryRoot ($relA.Replace('/', '\'))
    [IO.File]::WriteAllText($absA, "line1`nUNIQUE_ANCHOR_A`nline3`n", $utf8)
    $shaA = Get-FileSha $absA
    $manA = New-Manifest -RelativePath $relA -Ops @(@{ from = 'UNIQUE_ANCHOR_A'; to = 'UNIQUE_ANCHOR_B' }) -FileName 'a.json'
    $rA = Invoke-ApplyTextPatch -ManifestPath $manA -WhatIf
    Assert-True ($rA.ExitCode -eq 0) "A: exit esperado 0, veio $($rA.ExitCode). stderr=$($rA.StdErr)"
    Assert-True ($rA.Result.code -eq 'WHATIF_OK') "A: code WHATIF_OK"
    Assert-True ((Get-FileSha $absA) -eq $shaA) 'A: WhatIf não pode alterar SHA'

    # --- H apply unique (reuse file A) ---
    $rH = Invoke-ApplyTextPatch -ManifestPath $manA
    Assert-True ($rH.ExitCode -eq 0) "H: exit 0, veio $($rH.ExitCode)"
    Assert-True ($rH.Result.code -eq 'APPLIED') 'H: APPLIED'
    $textH = [IO.File]::ReadAllText($absA, $utf8)
    Assert-True ($textH.Contains('UNIQUE_ANCHOR_B') -and -not $textH.Contains('UNIQUE_ANCHOR_A')) 'H: substituição'
    Assert-True ((Get-EolKindBytes ([IO.File]::ReadAllBytes($absA))) -eq 'LF') 'H: preserva LF'

    # --- B duplicate ---
    $relB = Get-Rel 'dup.txt'
    $absB = Join-Path $repositoryRoot ($relB.Replace('/', '\'))
    [IO.File]::WriteAllText($absB, "x`nSHORT`ny`nSHORT`nz`n", $utf8)
    $shaB = Get-FileSha $absB
    $manB = New-Manifest -RelativePath $relB -Ops @(@{ from = 'SHORT'; to = 'LONG' }) -FileName 'b.json'
    $rB = Invoke-ApplyTextPatch -ManifestPath $manB
    Assert-True ($rB.ExitCode -eq 4) "B: exit ANCHOR_NOT_UNIQUE=4, veio $($rB.ExitCode)"
    Assert-True ($rB.Result.code -eq 'ANCHOR_NOT_UNIQUE') 'B: code'
    Assert-True ((Get-FileSha $absB) -eq $shaB) 'B: sem escrita'

    # --- C missing ---
    $relC = Get-Rel 'missing.txt'
    $absC = Join-Path $repositoryRoot ($relC.Replace('/', '\'))
    [IO.File]::WriteAllText($absC, "only other text`n", $utf8)
    $shaC = Get-FileSha $absC
    $manC = New-Manifest -RelativePath $relC -Ops @(@{ from = 'NO_SUCH'; to = 'X' }) -FileName 'c.json'
    $rC = Invoke-ApplyTextPatch -ManifestPath $manC
    Assert-True ($rC.ExitCode -eq 3) "C: exit 3, veio $($rC.ExitCode)"
    Assert-True ($rC.Result.code -eq 'ANCHOR_MISSING') 'C: code'
    Assert-True ((Get-FileSha $absC) -eq $shaC) 'C: sem escrita'

    # --- D batch partial ---
    $relD = Get-Rel 'batch.txt'
    $absD = Join-Path $repositoryRoot ($relD.Replace('/', '\'))
    [IO.File]::WriteAllText($absD, "P1_OLD`nP2_OLD`nP3_OLD`n", $utf8)
    $shaD = Get-FileSha $absD
    $manD = New-Manifest -RelativePath $relD -Ops @(
        @{ from = 'P1_OLD'; to = 'P1_NEW' },
        @{ from = 'P2_MISSING'; to = 'P2_NEW' },
        @{ from = 'P3_OLD'; to = 'P3_NEW' }
    ) -FileName 'd.json'
    $rD = Invoke-ApplyTextPatch -ManifestPath $manD
    Assert-True ($rD.ExitCode -in @(3, 8)) "D: exit 3 ou 8, veio $($rD.ExitCode)"
    Assert-True ((Get-FileSha $absD) -eq $shaD) 'D: arquivo intacto'
    Assert-True (-not ([IO.File]::ReadAllText($absD, $utf8).Contains('P1_NEW'))) 'D: P1 não aplicado'

    # --- E BOM ---
    $relE = Get-Rel 'bom.txt'
    $absE = Join-Path $repositoryRoot ($relE.Replace('/', '\'))
    [IO.File]::WriteAllBytes($absE, ([byte[]](0xEF, 0xBB, 0xBF) + $utf8.GetBytes("BOM_MARK`nbody`n")))
    $manE = New-Manifest -RelativePath $relE -Ops @(@{ from = 'BOM_MARK'; to = 'X' }) -FileName 'e.json'
    $rE = Invoke-ApplyTextPatch -ManifestPath $manE
    Assert-True ($rE.ExitCode -eq 7) "E: exit 7, veio $($rE.ExitCode)"
    Assert-True ($rE.Result.code -eq 'BOM_PRESENT') 'E: code'

    # --- F MIXED ---
    $relF = Get-Rel 'mixed.txt'
    $absF = Join-Path $repositoryRoot ($relF.Replace('/', '\'))
    [IO.File]::WriteAllBytes($absF, [Text.Encoding]::ASCII.GetBytes("a`r`nb`nc`r`n"))
    $shaF = Get-FileSha $absF
    $manF = New-Manifest -RelativePath $relF -Ops @(@{ from = 'a'; to = 'z' }) -FileName 'f.json'
    $rF = Invoke-ApplyTextPatch -ManifestPath $manF
    Assert-True ($rF.ExitCode -eq 5) "F: exit 5, veio $($rF.ExitCode)"
    Assert-True ($rF.Result.code -eq 'EOL_MIXED') 'F: code'
    Assert-True ((Get-FileSha $absF) -eq $shaF) 'F: sem escrita'

    # --- G CRLF .md × política LF → normaliza ---
    $relG = Get-Rel 'policy.md'
    $absG = Join-Path $repositoryRoot ($relG.Replace('/', '\'))
    [IO.File]::WriteAllText($absG, "alpha`r`nBETA_ONLY`r`ngamma`r`n", $utf8)
    Assert-True ((Get-EolKindBytes ([IO.File]::ReadAllBytes($absG))) -eq 'CRLF') 'G setup CRLF'
    $manG = New-Manifest -RelativePath $relG -Ops @(@{ from = 'BETA_ONLY'; to = 'BETA_DONE' }) -FileName 'g.json'
    $rG = Invoke-ApplyTextPatch -ManifestPath $manG
    Assert-True ($rG.ExitCode -eq 0) "G: exit 0, veio $($rG.ExitCode) stderr=$($rG.StdErr)"
    Assert-True ($rG.Result.code -eq 'APPLIED') 'G: APPLIED'
    Assert-True ([bool]$rG.Result.eolNormalized) 'G: eolNormalized'
    Assert-True ($rG.Result.eolAfter -eq 'lf') 'G: eolAfter lf'
    Assert-True ((Get-EolKindBytes ([IO.File]::ReadAllBytes($absG))) -eq 'LF') 'G: bytes LF'
    Assert-True ([IO.File]::ReadAllText($absG, $utf8).Contains('BETA_DONE')) 'G: texto'

    # --- path outside ---
    $manOut = New-Manifest -RelativePath '..\outside.txt' -Ops @(@{ from = 'a'; to = 'b' }) -FileName 'out.json'
    $rOut = Invoke-ApplyTextPatch -ManifestPath $manOut
    Assert-True ($rOut.ExitCode -eq 11) "path outside: exit 11, veio $($rOut.ExitCode)"
    Assert-True ($rOut.Result.code -eq 'PATH_OUTSIDE_REPO') 'path outside code'

    # --- duplicate from in manifesto ---
    $relDupOp = Get-Rel 'dupop.txt'
    $absDupOp = Join-Path $repositoryRoot ($relDupOp.Replace('/', '\'))
    [IO.File]::WriteAllText($absDupOp, "ONE`n", $utf8)
    $manDupOp = New-Manifest -RelativePath $relDupOp -Ops @(
        @{ from = 'ONE'; to = 'TWO' },
        @{ from = 'ONE'; to = 'THREE' }
    ) -FileName 'dupop.json'
    $rDupOp = Invoke-ApplyTextPatch -ManifestPath $manDupOp
    Assert-True ($rDupOp.ExitCode -eq 2) "dup from: exit 2, veio $($rDupOp.ExitCode)"
    Assert-True ($rDupOp.Result.code -eq 'INVALID_INPUT') 'dup from code'

    # --- CROSS_OP_TO_CONTAINS_FROM ---
    $relX = Get-Rel 'cross.txt'
    $absX = Join-Path $repositoryRoot ($relX.Replace('/', '\'))
    [IO.File]::WriteAllText($absX, "ALPHA_ONE`nBETA_TWO`n", $utf8)
    $shaX = Get-FileSha $absX
    $manX = New-Manifest -RelativePath $relX -Ops @(
        @{ from = 'ALPHA_ONE'; to = 'wrap BETA_TWO wrap' },
        @{ from = 'BETA_TWO'; to = 'DONE' }
    ) -FileName 'cross.json'
    $rX = Invoke-ApplyTextPatch -ManifestPath $manX
    Assert-True ($rX.ExitCode -eq 12) "cross: exit 12, veio $($rX.ExitCode)"
    Assert-True ($rX.Result.code -eq 'CROSS_OP_TO_CONTAINS_FROM') 'cross code'
    Assert-True ((Get-FileSha $absX) -eq $shaX) 'cross: sem escrita'

    # --- Concatenation hazard → ANCHOR_NOT_UNIQUE no buffer (D16.3) ---
    $relCat = Get-Rel 'concat.txt'
    $absCat = Join-Path $repositoryRoot ($relCat.Replace('/', '\'))
    [IO.File]::WriteAllText($absCat, "PREFIX_OLD_SUFFIX and PREFIX_SUFFIX`n", $utf8)
    $shaCat = Get-FileSha $absCat
    $manCat = New-Manifest -RelativePath $relCat -Ops @(
        @{ from = 'PREFIX_OLD'; to = 'PREFIX' },
        @{ from = 'PREFIX_SUFFIX'; to = 'GONE' }
    ) -FileName 'concat.json'
    $rCat = Invoke-ApplyTextPatch -ManifestPath $manCat
    Assert-True ($rCat.ExitCode -eq 4) "concat: exit 4, veio $($rCat.ExitCode) out=$($rCat.StdOut)"
    Assert-True ($rCat.Result.code -eq 'ANCHOR_NOT_UNIQUE') 'concat code'
    Assert-True ($rCat.Result.stage -eq 'buffer') 'concat stage=buffer'
    Assert-True ((Get-FileSha $absCat) -eq $shaCat) 'concat: sem escrita'
    Assert-True ([IO.File]::ReadAllText($absCat, $utf8).Contains('PREFIX_OLD_SUFFIX')) 'concat: original intacto'

    # Caso negativo exit 2 do script: harness ainda termina 0 (esta suíte)
    Assert-True ($rDupOp.ExitCode -eq 2) 'precondição: script saiu 2; harness não propaga'

    Write-Output 'PASS: Apply-TextPatch B122 suite'
    exit 0
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    if ($_.ScriptStackTrace) { [Console]::Error.WriteLine($_.ScriptStackTrace) }
    exit 1
}
finally {
    if (Test-Path -LiteralPath $fixtureRoot) {
        Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
