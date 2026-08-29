#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$installBatPath = Join-Path $PSScriptRoot '..\..\Install-ExtensionForGeneXus18.bat'
$u13InstallBatPath = Join-Path $PSScriptRoot '..\..\Install-ExtensionForGx18u13.bat'
$registerBatPath = Join-Path $PSScriptRoot '..\..\Register-ExtensionForGeneXus18.bat'
$u13RegisterBatPath = Join-Path $PSScriptRoot '..\..\Register-ExtensionForGx18u13.bat'
$copyScriptPath = Join-Path $PSScriptRoot '..\..\Tools\Copy-ExtensionForGeneXus18.ps1'

function Read-Source {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "SOURCE_MISSING: $Path"
    }

    return [IO.File]::ReadAllText($Path)
}

function Assert-NotContains {
    param([string]$Text, [string]$Unexpected, [string]$Message)

    if ($Text.IndexOf($Unexpected, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "ASSERT_NOT_CONTAINS_FAILED: $Message Unexpected='$Unexpected'"
    }
}

function Assert-Contains {
    param([string]$Text, [string]$Expected, [string]$Message)

    if ($Text.IndexOf($Expected, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "ASSERT_CONTAINS_FAILED: $Message Expected='$Expected'"
    }
}

$installBat = Read-Source $installBatPath
$u13InstallBat = Read-Source $u13InstallBatPath
$registerBat = Read-Source $registerBatPath
$u13RegisterBat = Read-Source $u13RegisterBatPath

Assert-NotContains $installBat 'if not exist "%GENEXUS_DIRECTORY%\GeneXus.exe" (' 'Install BAT não pode abrir bloco após expandir caminho com (x86).'
Assert-Contains $installBat 'if exist "%GENEXUS_DIRECTORY%\GeneXus.exe" goto geneXusFound' 'Install BAT deve validar o executável sem bloco parentizado.'
Assert-Contains $installBat ':geneXusFound' 'Install BAT deve manter o fluxo de sucesso após a validação do executável.'
Assert-Contains $installBat 'GeneXusDirectory "%GENEXUS_DIRECTORY%"' 'Install BAT deve encaminhar o diretório escolhido ao PowerShell.'
Assert-Contains $registerBat 'if exist "%GENEXUS_DIRECTORY%\GeneXus.exe" goto geneXusFound' 'Register BAT já usa o fluxo seguro para caminhos com (x86).'
Assert-Contains $registerBat 'GeneXus18"' 'Register canônico deve defaultar para GeneXus18 (U14+).'
Assert-NotContains $registerBat 'GeneXus18up13' 'Register canônico não deve defaultar para U13.'

Assert-Contains $u13InstallBat 'artifacts\gx18u13\bin\Release\net471\GenexusOpenApiBuilder.Extension.dll' 'Install U13 deve apontar para a DLL satélite, não para a build canônica.'
Assert-Contains $u13InstallBat '-BuildDll "%BUILD_DLL%" -GeneXusDirectory "%GENEXUS_DIRECTORY%"' 'Install U13 deve copiar usando explicitamente a DLL satélite e o diretório escolhido.'
Assert-Contains $u13InstallBat '-BuildDll "%BUILD_DLL%" -InstalledDll' 'Install U13 deve validar o hash contra a mesma DLL satélite usada na cópia.'
Assert-Contains $u13InstallBat 'if exist "%GENEXUS_DIRECTORY%\GeneXus.exe" goto geneXusFound' 'Install U13 deve validar caminhos com (x86) sem bloco parentizado.'
Assert-Contains $installBat 'Register-ExtensionForGeneXus18.bat "%GENEXUS_DIRECTORY%"' 'Install canônico deve orientar o Register com o mesmo diretório da cópia.'
Assert-Contains $u13InstallBat 'Register-ExtensionForGx18u13.bat "%GENEXUS_DIRECTORY%"' 'Install U13 deve orientar o Register U13 com o mesmo diretório da cópia satélite.'
Assert-NotContains $installBat 'Register-ExtensionForGeneXus18.bat normalmente' 'Install canônico não pode omitir o diretório no eco do Register.'
Assert-NotContains $u13InstallBat 'Register-ExtensionForGeneXus18.bat normalmente' 'Install U13 não pode omitir o diretório no eco do Register.'

Assert-Contains $u13RegisterBat 'GeneXus18up13' 'Register U13 deve defaultar para GeneXus18up13.'
Assert-Contains $u13RegisterBat 'if exist "%GENEXUS_DIRECTORY%\GeneXus.exe" goto geneXusFound' 'Register U13 deve validar caminhos com (x86) sem bloco parentizado.'
Assert-Contains $u13RegisterBat 'genexus /install' 'Register U13 deve orientar genexus /install.'
Assert-NotContains $u13RegisterBat 'GeneXus18"' 'Register U13 não deve defaultar para GeneXus18 canônico.'

$copyScript = Read-Source $copyScriptPath
Assert-Contains $copyScript 'Register-ExtensionForGx18u13.bat' 'Copy-Extension deve nomear o Register satélite U13.'
Assert-Contains $copyScript 'Register-ExtensionForGeneXus18.bat' 'Copy-Extension deve nomear o Register canônico U14+.'
Assert-Contains $copyScript 'gx18u13' 'Copy-Extension deve detectar a linha satélite pela pasta gx18u13 na BuildDll.'
Assert-Contains $copyScript '$registerBatName' 'Copy-Extension deve escolher o Register pela linha detectada.'

Write-Output 'PASS: InstallExtensionBatPathHandling'
