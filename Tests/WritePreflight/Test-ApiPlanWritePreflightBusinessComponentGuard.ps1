Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$preflightPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanWritePreflight.cs'
$source = Get-Content -LiteralPath $preflightPath -Raw
$guardPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanBusinessComponentEnablementGuard.cs'

function Assert-Contains {
    param([string]$Text, [string]$Expected, [string]$Message)
    if (-not $Text.Contains($Expected)) {
        throw "ASSERT_CONTAINS_FAILED: $Message Expected='$Expected'"
    }
}

function Assert-Throws {
    param([scriptblock]$Action, [string]$Expected, [string]$Message)
    try {
        & $Action
    }
    catch {
        if ($_.Exception.Message -like "*$Expected*") {
            return
        }

        throw "ASSERT_THROW_FAILED: $Message Expected='$Expected' Actual='$($_.Exception.Message)'"
    }

    throw "ASSERT_THROW_FAILED: $Message Expected='$Expected' Nenhuma excecao foi lancada."
}

$methodStart = $source.IndexOf('internal static void ValidateForF1(', [StringComparison]::Ordinal)
$nextMethodStart = $source.IndexOf('private static void ValidateForIntentionalChange(', [StringComparison]::Ordinal)
if ($methodStart -lt 0 -or $nextMethodStart -le $methodStart) {
    throw 'ASSERT_SECTION_FAILED: nao foi possivel isolar ValidateForF1.'
}

$methodSource = $source.Substring($methodStart, $nextMethodStart - $methodStart)
Assert-Contains $methodSource 'ApiPlanBusinessComponentEnablementGuard.ThrowIfBlocked(' 'F1 deve delegar o guard B055 a uma regra comportamental testável.'

$guardIndex = $methodSource.IndexOf('ApiPlanBusinessComponentEnablementGuard.ThrowIfBlocked(', [StringComparison]::Ordinal)
$consumerValidationIndex = $methodSource.IndexOf('if (generateSdts && requiresConsumersOrApi)', [StringComparison]::Ordinal)
if ($guardIndex -lt 0 -or $consumerValidationIndex -lt 0 -or $guardIndex -ge $consumerValidationIndex) {
    throw 'ASSERT_ORDER_FAILED: o guard de BC deve ocorrer antes das validacoes de writers consumidores.'
}

if (-not (Test-Path -LiteralPath $guardPath)) {
    throw "ASSERT_FILE_FAILED: guard B055 nao encontrado em '$guardPath'."
}

$guardType = Add-Type -Path $guardPath -PassThru
$throwIfBlocked = $guardType.GetMethod('ThrowIfBlocked', [System.Reflection.BindingFlags]'Static, NonPublic')
if ($null -eq $throwIfBlocked) {
    throw 'ASSERT_METHOD_FAILED: metodo comportamental ThrowIfBlocked nao encontrado.'
}

function Invoke-B055Guard {
    param([bool]$ApplyBusinessComponent, [bool]$IsBusinessComponent, [bool]$EnablementPending)
    try {
        [void]$throwIfBlocked.Invoke($null, @($ApplyBusinessComponent, $IsBusinessComponent, $EnablementPending, 'Teste'))
    }
    catch [System.Reflection.TargetInvocationException] {
        throw $_.Exception.InnerException
    }
}

Assert-Throws { Invoke-B055Guard $true $false $false } 'B055 bloqueado: Transaction=' 'BC desabilitado sem habilitação pendente deve bloquear antes dos consumidores.'
Invoke-B055Guard $true $false $true
Invoke-B055Guard $true $true $false
Invoke-B055Guard $false $false $false

Write-Output 'PASS: ApiPlanWritePreflightBusinessComponentGuard'
