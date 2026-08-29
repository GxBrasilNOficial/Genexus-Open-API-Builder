[CmdletBinding()]
param(
    [switch]$Apply,
    [string]$BuildDll = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Src\Extension\bin\Release\net471\GenexusOpenApiBuilder.Extension.dll'),
    [string]$GeneXusDirectory = 'C:\Program Files (x86)\GeneXus\GeneXus18',
    [string]$LogDirectory = 'C:\Temp'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$extensionFileName = 'GenexusOpenApiBuilder.Extension.dll'
$targetDll = Join-Path $GeneXusDirectory "Packages\$extensionFileName"
$geneXusExe = Join-Path $GeneXusDirectory 'GeneXus.exe'

# Linha satélite U13: DLL sob artifacts\gx18u13 ou diretório GeneXus18up13.
$isGx18u13 = ($BuildDll -match '(?i)[\\/]gx18u13[\\/]') -or
    ($GeneXusDirectory -match '(?i)GeneXus18up13')
$installBatName = if ($isGx18u13) { 'Install-ExtensionForGx18u13.bat' } else { 'Install-ExtensionForGeneXus18.bat' }
$registerBatName = if ($isGx18u13) { 'Register-ExtensionForGx18u13.bat' } else { 'Register-ExtensionForGeneXus18.bat' }

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
        NextCommand = $installBatName
        FollowingCommand = $registerBatName
        RegistrationInput = 'genexus /install; depois exit'
        OperationalNote = 'O PowerShell apenas copia e valida. O registro ocorre somente pelo segundo .bat, sem Administrador.'
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
    throw 'Feche completamente a IDE GeneXus antes de copiar a extensão.'
}

if (-not (Test-Path -LiteralPath $LogDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupDll = Join-Path $LogDirectory "GenexusOpenApiBuilder.Extension.backup-$timestamp.dll"

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

[pscustomobject]@{
    InstalledMatchesBuild = ($installedHash -eq $buildHash)
    BuildDll = $BuildDll
    InstalledDll = $targetDll
    BuildSha256 = $buildHash
    InstalledSha256 = $installedHash
    BackupDll = $backupPath
    RegistrationDeferred = $true
    NextCommand = $registerBatName
}
