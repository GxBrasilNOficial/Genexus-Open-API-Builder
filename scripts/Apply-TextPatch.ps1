#requires -Version 7.4
<#
.SYNOPSIS
    Aplica um lote atômico de substituições textuais ancoradas (B122).

.DESCRIPTION
    Recebe um manifesto JSON com path relativo à raiz do repositório e ops {from,to}.
    Valida todas as âncoras no original, aplica fail-fast cruzado to⊃from, revalida
    count==1 no buffer antes de cada substituição, e só então grava (salvo -WhatIf).

    Não substitui Apply-ApprovedPatch (backup de unified diff Git nas skills XPZ).
    Esta ferramenta cobre edição ancorada local neste repositório.

.NOTES
    Plano: Docs/Implementation/2026-09-18-B122-PLANO-EDICAO-TEXTUAL-ANCORADA.md
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ManifestPath,

    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:ExitCodes = @{
    INTERNAL_ERROR             = 1
    INVALID_INPUT              = 2
    ANCHOR_MISSING             = 3
    ANCHOR_NOT_UNIQUE          = 4
    EOL_MIXED                  = 5
    EOL_NONE                   = 6
    BOM_PRESENT                = 7
    BATCH_VALIDATION_FAILED    = 8
    WRITE_FAILED               = 9
    GIT_ATTR_FAILED            = 10
    PATH_OUTSIDE_REPO          = 11
    CROSS_OP_TO_CONTAINS_FROM  = 12
}

function Get-Sha256Hex {
    param([byte[]]$Bytes)
    return (([System.Security.Cryptography.SHA256]::HashData($Bytes) | ForEach-Object { $_.ToString('x2') }) -join '')
}

function Write-PatchResult {
    param(
        [string]$Status,
        [string]$Code,
        [string]$Message = '',
        [hashtable]$Data = @{}
    )

    $payload = [ordered]@{
        kind          = 'apply-text-patch-result'
        schemaVersion = 1
        status        = $Status
        code          = $Code
        message       = $Message
        whatIf        = [bool]$WhatIf
    }
    foreach ($entry in $Data.GetEnumerator()) {
        $payload[$entry.Key] = $entry.Value
    }
    [Console]::Out.WriteLine(($payload | ConvertTo-Json -Compress -Depth 12))
}

function Stop-Patch {
    param(
        [string]$Code,
        [string]$Message,
        [hashtable]$Data = @{}
    )

    Write-PatchResult -Status 'error' -Code $Code -Message $Message -Data $Data
    if (-not $script:ExitCodes.ContainsKey($Code)) {
        exit $script:ExitCodes.INTERNAL_ERROR
    }
    exit $script:ExitCodes[$Code]
}

function Get-OccurrenceCount {
    param([string]$Text, [string]$Needle)
    if ([string]::IsNullOrEmpty($Needle)) { return -1 }
    return ([regex]::Matches($Text, [regex]::Escape($Needle))).Count
}

function Get-EolKind {
    param([byte[]]$Bytes)
    $crlf = 0
    $lf = 0
    $cr = 0
    for ($i = 0; $i -lt $Bytes.Length; $i++) {
        if ($Bytes[$i] -eq 10) {
            if ($i -gt 0 -and $Bytes[$i - 1] -eq 13) { $crlf++ }
            else { $lf++ }
        }
        elseif ($Bytes[$i] -eq 13 -and ($i + 1 -ge $Bytes.Length -or $Bytes[$i + 1] -ne 10)) {
            $cr++
        }
    }
    if ($crlf -gt 0 -and $lf -eq 0 -and $cr -eq 0) { return 'CRLF' }
    if ($lf -gt 0 -and $crlf -eq 0 -and $cr -eq 0) { return 'LF' }
    if ($crlf -eq 0 -and $lf -eq 0 -and $cr -eq 0) { return 'NONE' }
    return 'MIXED'
}

function Test-HasUtf8Bom {
    param([byte[]]$Bytes)
    return ($Bytes.Length -ge 3 -and $Bytes[0] -eq 0xEF -and $Bytes[1] -eq 0xBB -and $Bytes[2] -eq 0xBF)
}

function Convert-TextToEol {
    param([string]$Text, [ValidateSet('LF', 'CRLF')][string]$Eol)
    $normalized = $Text -replace "`r`n", "`n" -replace "`r", "`n"
    if ($Eol -eq 'CRLF') {
        return ($normalized -replace "`n", "`r`n")
    }
    return $normalized
}

function Replace-FirstOccurrence {
    param([string]$Text, [string]$From, [string]$To)
    $idx = $Text.IndexOf($From, [StringComparison]::Ordinal)
    if ($idx -lt 0) {
        throw "Replace-FirstOccurrence: âncora ausente."
    }
    return $Text.Substring(0, $idx) + $To + $Text.Substring($idx + $From.Length)
}

function Resolve-RepositoryRoot {
    $candidate = Split-Path -Parent $PSScriptRoot
    $git = & git -C $candidate rev-parse --show-toplevel 2>&1
    if ($LASTEXITCODE -ne 0) {
        Stop-Patch 'GIT_ATTR_FAILED' "Não foi possível resolver a raiz Git a partir de $candidate." @{ diagnostic = @($git | ForEach-Object { "$_" }) }
    }
    return [IO.Path]::GetFullPath(($git | Select-Object -First 1).ToString().Trim())
}

function Resolve-RepoRelativePath {
    param([string]$RepositoryRoot, [string]$RawPath)

    if ([string]::IsNullOrWhiteSpace($RawPath)) {
        Stop-Patch 'INVALID_INPUT' 'path do manifesto está vazio.'
    }
    if ($RawPath.Contains([char]0)) {
        Stop-Patch 'INVALID_INPUT' 'path contém NUL.'
    }

    $normalized = $RawPath.Replace('\', '/')
    if ([IO.Path]::IsPathRooted($RawPath) -or $normalized -match '^[A-Za-z]:') {
        Stop-Patch 'PATH_OUTSIDE_REPO' "path deve ser relativo à raiz do repositório: $RawPath"
    }

    $parts = @($normalized -split '/')
    if ($parts.Count -eq 0 -or @($parts | Where-Object { $_ -in @('', '.', '..') }).Count -gt 0) {
        Stop-Patch 'PATH_OUTSIDE_REPO' "path inválido ou com '..': $RawPath"
    }

    $relative = $parts -join [IO.Path]::DirectorySeparatorChar
    $absolute = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot $relative))
    $rootFull = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\', '/')
    if (-not $absolute.StartsWith($rootFull + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -and
        -not ($absolute.Equals($rootFull, [StringComparison]::OrdinalIgnoreCase))) {
        Stop-Patch 'PATH_OUTSIDE_REPO' "path resolve fora do repositório: $RawPath"
    }

    $gitRelative = ($parts -join '/')
    return [pscustomobject]@{
        Absolute   = $absolute
        GitRelative = $gitRelative
    }
}

function Get-EolPolicy {
    param([string]$RepositoryRoot, [string]$GitRelativePath)

    $attr = & git -C $RepositoryRoot check-attr eol -- $GitRelativePath 2>&1
    if ($LASTEXITCODE -ne 0) {
        Stop-Patch 'GIT_ATTR_FAILED' "git check-attr falhou para $GitRelativePath." @{ diagnostic = @($attr | ForEach-Object { "$_" }) }
    }
    $line = @($attr | ForEach-Object { "$_" }) | Select-Object -First 1
    if ($line -match ':\s*eol:\s*lf\s*$') { return 'lf' }
    if ($line -match ':\s*eol:\s*crlf\s*$') { return 'crlf' }
    return 'preserve'
}

try {
    if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
        Stop-Patch 'INVALID_INPUT' "ManifestPath não encontrado: $ManifestPath"
    }

    $manifestBytes = [IO.File]::ReadAllBytes($ManifestPath)
    if (Test-HasUtf8Bom $manifestBytes) {
        Stop-Patch 'INVALID_INPUT' 'Manifesto não pode ter BOM UTF-8.'
    }

    try {
        $manifestText = [Text.UTF8Encoding]::new($false, $true).GetString($manifestBytes)
        $manifest = $manifestText | ConvertFrom-Json
    }
    catch {
        Stop-Patch 'INVALID_INPUT' "Manifesto JSON inválido: $($_.Exception.Message)"
    }

    if ($null -eq $manifest -or $null -eq $manifest.schemaVersion -or [int]$manifest.schemaVersion -ne 1) {
        Stop-Patch 'INVALID_INPUT' 'schemaVersion deve ser 1.'
    }
    if ($null -eq $manifest.path -or [string]::IsNullOrWhiteSpace([string]$manifest.path)) {
        Stop-Patch 'INVALID_INPUT' 'path é obrigatório.'
    }
    if ($null -eq $manifest.ops) {
        Stop-Patch 'INVALID_INPUT' 'ops é obrigatório.'
    }

    $opsRaw = @($manifest.ops)
    if ($opsRaw.Count -eq 0) {
        Stop-Patch 'INVALID_INPUT' 'ops não pode ser vazio.'
    }

    $ops = [System.Collections.Generic.List[object]]::new()
    $fromSet = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    for ($i = 0; $i -lt $opsRaw.Count; $i++) {
        $item = $opsRaw[$i]
        if ($null -eq $item.from -or $null -eq $item.to) {
            Stop-Patch 'INVALID_INPUT' "ops[$i] exige from e to."
        }
        $from = [string]$item.from
        $to = [string]$item.to
        if ($from.Length -eq 0) {
            Stop-Patch 'INVALID_INPUT' "ops[$i].from não pode ser vazio."
        }
        if ($from -ceq $to) {
            Stop-Patch 'INVALID_INPUT' "ops[$i]: from e to são idênticos."
        }
        if (-not $fromSet.Add($from)) {
            Stop-Patch 'INVALID_INPUT' "ops[$i]: from duplicada no manifesto."
        }
        [void]$ops.Add([pscustomobject]@{ index = $i; from = $from; to = $to })
    }

    $repositoryRoot = Resolve-RepositoryRoot
    $resolved = Resolve-RepoRelativePath -RepositoryRoot $repositoryRoot -RawPath ([string]$manifest.path)
    if (-not (Test-Path -LiteralPath $resolved.Absolute -PathType Leaf)) {
        Stop-Patch 'INVALID_INPUT' "Arquivo alvo não encontrado: $($resolved.GitRelative)"
    }

    $fileBytes = [IO.File]::ReadAllBytes($resolved.Absolute)
    $shaBefore = Get-Sha256Hex $fileBytes
    if (Test-HasUtf8Bom $fileBytes) {
        Stop-Patch 'BOM_PRESENT' "Arquivo alvo tem BOM UTF-8: $($resolved.GitRelative)"
    }

    $eolBefore = Get-EolKind $fileBytes
    if ($eolBefore -eq 'MIXED') {
        Stop-Patch 'EOL_MIXED' "Arquivo alvo tem EOL misto: $($resolved.GitRelative)"
    }
    if ($eolBefore -eq 'NONE') {
        Stop-Patch 'EOL_NONE' "Arquivo alvo sem newline; v1 recusa ops: $($resolved.GitRelative)"
    }

    $eolPolicy = Get-EolPolicy -RepositoryRoot $repositoryRoot -GitRelativePath $resolved.GitRelative

    try {
        $originalText = [Text.UTF8Encoding]::new($false, $true).GetString($fileBytes)
    }
    catch {
        Stop-Patch 'INVALID_INPUT' "Arquivo alvo não é UTF-8 válido: $($resolved.GitRelative)"
    }

    # Passo 1 — âncoras no original
    $anchorErrors = [System.Collections.Generic.List[object]]::new()
    foreach ($op in $ops) {
        $count = Get-OccurrenceCount -Text $originalText -Needle $op.from
        if ($count -eq 0) {
            [void]$anchorErrors.Add([ordered]@{ index = $op.index; code = 'ANCHOR_MISSING'; count = 0 })
        }
        elseif ($count -ne 1) {
            [void]$anchorErrors.Add([ordered]@{ index = $op.index; code = 'ANCHOR_NOT_UNIQUE'; count = $count })
        }
    }
    if ($anchorErrors.Count -eq 1) {
        $only = $anchorErrors[0]
        Stop-Patch $only.code "Âncora inválida no original (ops[$($only.index)])." @{ errors = @($only); path = $resolved.GitRelative }
    }
    if ($anchorErrors.Count -gt 1) {
        Stop-Patch 'BATCH_VALIDATION_FAILED' 'Múltiplas âncoras inválidas no original.' @{ errors = @($anchorErrors.ToArray()); path = $resolved.GitRelative }
    }

    # Passo 2 — fail-fast to⊃from cruzado
    for ($i = 0; $i -lt $ops.Count; $i++) {
        for ($j = 0; $j -lt $ops.Count; $j++) {
            if ($i -eq $j) { continue }
            if ($ops[$i].to.IndexOf($ops[$j].from, [StringComparison]::Ordinal) -ge 0) {
                Stop-Patch 'CROSS_OP_TO_CONTAINS_FROM' "ops[$i].to contém ops[$j].from." @{
                    path   = $resolved.GitRelative
                    toIndex = $i
                    fromIndex = $j
                }
            }
        }
    }

    # Passo 3 — apply em memória com revalidação
    $buffer = $originalText
    $opReceipts = [System.Collections.Generic.List[object]]::new()
    foreach ($op in $ops) {
        $count = Get-OccurrenceCount -Text $buffer -Needle $op.from
        if ($count -eq 0) {
            Stop-Patch 'ANCHOR_MISSING' "Âncora ausente no buffer antes de ops[$($op.index)]." @{
                path  = $resolved.GitRelative
                index = $op.index
                stage = 'buffer'
            }
        }
        if ($count -ne 1) {
            Stop-Patch 'ANCHOR_NOT_UNIQUE' "Âncora não única no buffer antes de ops[$($op.index)] (count=$count)." @{
                path  = $resolved.GitRelative
                index = $op.index
                count = $count
                stage = 'buffer'
            }
        }
        $buffer = Replace-FirstOccurrence -Text $buffer -From $op.from -To $op.to
        [void]$opReceipts.Add([ordered]@{
                index            = $op.index
                fromLength       = $op.from.Length
                toLength         = $op.to.Length
                occurrenceCount  = 1
            })
    }

    # EOL de saída
    $eolNormalized = $false
    $eolAfterKind = $eolBefore
    $writeEol = $null
    if ($eolPolicy -eq 'lf') {
        $writeEol = 'LF'
        if ($eolBefore -ne 'LF') { $eolNormalized = $true }
        $eolAfterKind = 'LF'
    }
    elseif ($eolPolicy -eq 'crlf') {
        $writeEol = 'CRLF'
        if ($eolBefore -ne 'CRLF') { $eolNormalized = $true }
        $eolAfterKind = 'CRLF'
    }
    else {
        $writeEol = $eolBefore
        $eolAfterKind = $eolBefore
    }

    $finalText = Convert-TextToEol -Text $buffer -Eol $writeEol
    $utf8 = [Text.UTF8Encoding]::new($false)
    $finalBytes = $utf8.GetBytes($finalText)
    $shaAfter = Get-Sha256Hex $finalBytes

    $common = @{
        path          = $resolved.GitRelative
        opsApplied    = $ops.Count
        eolPolicy     = $eolPolicy
        eolBefore     = $eolBefore.ToLowerInvariant()
        eolAfter      = $eolAfterKind.ToLowerInvariant()
        eolNormalized = $eolNormalized
        sha256Before  = $shaBefore
        sha256After   = $shaAfter
        ops           = @($opReceipts.ToArray())
    }

    if ($WhatIf) {
        Write-PatchResult -Status 'ok' -Code 'WHATIF_OK' -Message 'Validação completa; nenhuma escrita.' -Data $common
        exit 0
    }

    try {
        [IO.File]::WriteAllBytes($resolved.Absolute, $finalBytes)
    }
    catch {
        Stop-Patch 'WRITE_FAILED' "Falha ao gravar $($resolved.GitRelative): $($_.Exception.Message)"
    }

    $written = [IO.File]::ReadAllBytes($resolved.Absolute)
    $common['sha256After'] = Get-Sha256Hex $written
    Write-PatchResult -Status 'ok' -Code 'APPLIED' -Message 'Substituições aplicadas.' -Data $common
    exit 0
}
catch {
    if ($_.Exception.Message -match '^(INVALID_INPUT|ANCHOR_|EOL_|BOM_|BATCH_|WRITE_|GIT_|PATH_|CROSS_|INTERNAL_)') {
        throw
    }
    [Console]::Error.WriteLine($_.Exception.Message)
    Write-PatchResult -Status 'error' -Code 'INTERNAL_ERROR' -Message 'Erro interno inesperado.'
    exit $script:ExitCodes.INTERNAL_ERROR
}
