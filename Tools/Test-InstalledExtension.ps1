[CmdletBinding()]
param(
    [string]$BuildDll,
    [string]$InstalledDll
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($BuildDll)) {
    $BuildDll = Join-Path (Split-Path -Parent $PSScriptRoot) 'Src\Extension\bin\Release\net471\GenexusOpenApiBuilder.Extension.dll'
}

if ([string]::IsNullOrWhiteSpace($InstalledDll)) {
    if (Test-Path 'C:\GeneXus\Gx18\U13\Packages\GenexusOpenApiBuilder.Extension.dll') {
        $InstalledDll = 'C:\GeneXus\Gx18\U13\Packages\GenexusOpenApiBuilder.Extension.dll'
    } else {
        $InstalledDll = 'C:\Program Files (x86)\GeneXus\GeneXus18\Packages\GenexusOpenApiBuilder.Extension.dll'
    }
}

function Require-File {
    param(
        [Parameter(Mandatory)] [string]$Path,
        [Parameter(Mandatory)] [string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Label não encontrado: $Path"
    }
}

Require-File -Path $BuildDll -Label 'DLL compilada'
Require-File -Path $InstalledDll -Label 'DLL instalada'

$buildHash = (Get-FileHash -LiteralPath $BuildDll -Algorithm SHA256).Hash
$installedHash = (Get-FileHash -LiteralPath $InstalledDll -Algorithm SHA256).Hash

if ($installedHash -ne $buildHash) {
    throw "Validação falhou: a DLL em '$InstalledDll' (SHA256 $installedHash) não corresponde à DLL compilada em '$BuildDll' (SHA256 $buildHash)."
}

$buildInfo = Get-Item -LiteralPath $BuildDll
$installedInfo = Get-Item -LiteralPath $InstalledDll
$buildVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($BuildDll)
$installedVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($InstalledDll)

[pscustomobject]@{
    Status = 'OK'
    InstalledMatchesBuild = $true
    BuildDll = $BuildDll
    InstalledDll = $InstalledDll
    Sha256 = $buildHash
    BuildLastWriteTime = $buildInfo.LastWriteTime
    InstalledLastWriteTime = $installedInfo.LastWriteTime
    FileVersion = $installedVersion.FileVersion
    ProductVersion = $installedVersion.ProductVersion
}