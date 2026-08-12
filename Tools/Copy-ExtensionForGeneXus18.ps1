[CmdletBinding()]
param(
    [switch]$Apply,
    [string]$BuildDll,
    [string]$GeneXusDirectory,
    [string]$LogDirectory = 'C:\Temp'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($BuildDll)) {
    $BuildDll = Join-Path (Split-Path -Parent $PSScriptRoot) 'Src\Extension\bin\Release\net471\GenexusOpenApiBuilder.Extension.dll'
}

if ([string]::IsNullOrWhiteSpace($GeneXusDirectory)) {
    if (Test-Path 'C:\GeneXus\Gx18\U13\GeneXus.exe') {
        $GeneXusDirectory = 'C:\GeneXus\Gx18\U13'
    } else {
        $GeneXusDirectory = 'C:\Program Files (x86)\GeneXus\GeneXus18'
    }
}

$extensionFileName = 'GenexusOpenApiBuilder.Extension.dll'
$packageFileName   = 'GenexusOpenApiBuilder.Extension.package'
$targetDll = Join-Path $GeneXusDirectory "Packages\$extensionFileName"
$targetPackage = Join-Path $GeneXusDirectory "Packages\$packageFileName"
$geneXusExe = Join-Path $GeneXusDirectory 'GeneXus.exe'
$sourcePackage = Join-Path (Split-Path -Parent $PSScriptRoot) 'Src\Extension\Package.package'

if (-not (Test-Path -LiteralPath $BuildDll -PathType Leaf)) {
    throw "DLL compilada não encontrada: $BuildDll"
}

if (-not (Test-Path -LiteralPath $geneXusExe -PathType Leaf)) {
    throw "Executável do GeneXus não encontrado: $geneXusExe"
}

$buildHash = (Get-FileHash -LiteralPath $BuildDll -Algorithm SHA256).Hash

if (-not $Apply) {
    [pscustomobject]@{
        ApplyRequired = $true
        BuildDll = $BuildDll
        BuildSha256 = $buildHash
        TargetDll = $targetDll
        GeneXusDirectory = $GeneXusDirectory
    }
    return
}

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Execute este script em um PowerShell aberto como Administrador.'
}

$runningGeneXus = @(Get-Process -Name GeneXus -ErrorAction SilentlyContinue)
if ($runningGeneXus.Count -gt 0) {
    throw 'Feche completamente a IDE GeneXus antes de copiar a extensão.'
}

Copy-Item -LiteralPath $BuildDll -Destination $targetDll -Force
if (Test-Path -LiteralPath $sourcePackage -PathType Leaf) {
    Copy-Item -LiteralPath $sourcePackage -Destination $targetPackage -Force
}

$installedHash = (Get-FileHash -LiteralPath $targetDll -Algorithm SHA256).Hash
if ($installedHash -ne $buildHash) {
    throw 'A DLL copiada para Packages não corresponde à DLL compilada.'
}

[pscustomobject]@{
    InstalledMatchesBuild = ($installedHash -eq $buildHash)
    BuildDll = $BuildDll
    InstalledDll = $targetDll
    InstalledPackage = $targetPackage
}