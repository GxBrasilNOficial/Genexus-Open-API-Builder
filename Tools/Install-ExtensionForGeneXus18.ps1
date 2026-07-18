[CmdletBinding()]
param(
    [switch]$Apply,
    [switch]$SkipGeneXusInstall,
    [string]$BuildDll = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Src\Extension\bin\Release\net471\GenexusOpenApiBuilder.Extension.dll'),
    [string]$GeneXusDirectory = 'C:\Program Files (x86)\GeneXus\GeneXus18',
    [string]$LogDirectory = 'C:\Temp'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$extensionFileName = 'GenexusOpenApiBuilder.Extension.dll'
$targetDll = Join-Path $GeneXusDirectory "Packages\$extensionFileName"
$geneXusExe = Join-Path $GeneXusDirectory 'GeneXus.exe'

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
        NextCommand = 'pwsh -NoProfile -File Tools/Install-ExtensionForGeneXus18.ps1 -Apply'
    }
    return
}

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
$administratorRole = [Security.Principal.WindowsBuiltInRole]::Administrator
if (-not $principal.IsInRole($administratorRole)) {
    throw 'Execute este script em um PowerShell aberto como Administrador.'
}

$runningGeneXus = @(Get-Process -Name GeneXus -ErrorAction SilentlyContinue)
if ($runningGeneXus.Count -gt 0) {
    throw 'Feche completamente a IDE GeneXus antes de instalar a extensão.'
}

if (-not (Test-Path -LiteralPath $LogDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupDll = Join-Path $LogDirectory "GenexusOpenApiBuilder.Extension.backup-$timestamp.dll"
$installLog = Join-Path $LogDirectory "GenexusOpenApiBuilder.Extension.install-$timestamp.log"

if (Test-Path -LiteralPath $targetDll -PathType Leaf) {
    Copy-Item -LiteralPath $targetDll -Destination $backupDll -Force
}

Copy-Item -LiteralPath $BuildDll -Destination $targetDll -Force
$installedHash = (Get-FileHash -LiteralPath $targetDll -Algorithm SHA256).Hash
if ($installedHash -ne $buildHash) {
    throw 'A DLL copiada para Packages não corresponde à DLL compilada.'
}

$backupPath = $null
if (Test-Path -LiteralPath $backupDll -PathType Leaf) {
    $backupPath = $backupDll
}

if ($SkipGeneXusInstall) {
    [pscustomobject]@{
        InstalledMatchesBuild = ($installedHash -eq $buildHash)
        GeneXusInstallDeferred = $true
        BackupDll = $backupPath
        InstallLog = $null
    }
    return
}

Push-Location -LiteralPath $GeneXusDirectory
try {
    $installOutput = @(& $env:ComSpec /d /c 'genexus.exe /install' 2>&1 | ForEach-Object { $_.ToString() })
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

Set-Content -LiteralPath $installLog -Value $installOutput -Encoding utf8
$addedLine = "Package '$extensionFileName' added"
$compatibilityError = "Compatibility: cannot load package 'GenexusOpenApiBuilder.Extension.Package'"
$attributeError = "Package Attribute not found '$extensionFileName'"
$consoleOutputCaptured = $installOutput.Count -gt 0
$wasAdded = $null
$hasCompatibilityError = $false
$hasAttributeError = $false
if ($consoleOutputCaptured) {
    $wasAdded = $installOutput -contains $addedLine
    $hasCompatibilityError = [bool]($installOutput | Where-Object { $_ -like "$compatibilityError*" })
    $hasAttributeError = $installOutput -contains $attributeError
}
$result = [pscustomobject]@{
    InstalledMatchesBuild = ($installedHash -eq $buildHash)
    GeneXusInstallDeferred = $false
    PackageAdded = $wasAdded
    ConsoleOutputCaptured = $consoleOutputCaptured
    ManualConsoleReviewRequired = (-not $consoleOutputCaptured)
    CompatibilityError = [bool]$hasCompatibilityError
    PackageAttributeError = $hasAttributeError
    ExitCode = $exitCode
    BackupDll = $backupPath
    InstallLog = $installLog
}

$result

if (($result.ConsoleOutputCaptured -and -not $result.PackageAdded) -or $result.CompatibilityError -or $result.PackageAttributeError -or $result.ExitCode -ne 0) {
    Write-Error 'A instalação não passou na validação. Consulte InstallLog e restaure BackupDll se necessário.'
    exit 1
}
