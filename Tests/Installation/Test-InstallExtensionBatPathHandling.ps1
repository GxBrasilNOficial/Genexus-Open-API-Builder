#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$installBatPath = Join-Path $PSScriptRoot '..\..\Install-ExtensionForGeneXus18.bat'
$u13InstallBatPath = Join-Path $PSScriptRoot '..\..\Install-ExtensionForGx18u13.bat'
$registerBatPath = Join-Path $PSScriptRoot '..\..\Register-ExtensionForGeneXus18.bat'

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

Assert-NotContains $installBat 'if not exist "%GENEXUS_DIRECTORY%\GeneXus.exe" (' 'Install BAT não pode abrir bloco após expandir caminho com (x86).'
Assert-Contains $installBat 'if exist "%GENEXUS_DIRECTORY%\GeneXus.exe" goto geneXusFound' 'Install BAT deve validar o executável sem bloco parentizado.'
Assert-Contains $installBat ':geneXusFound' 'Install BAT deve manter o fluxo de sucesso após a validação do executável.'
Assert-Contains $installBat 'GeneXusDirectory "%GENEXUS_DIRECTORY%"' 'Install BAT deve encaminhar o diretório escolhido ao PowerShell.'
Assert-Contains $registerBat 'if exist "%GENEXUS_DIRECTORY%\GeneXus.exe" goto geneXusFound' 'Register BAT já usa o fluxo seguro para caminhos com (x86).'

Assert-Contains $u13InstallBat 'artifacts\gx18u13\bin\Release\net471\GenexusOpenApiBuilder.Extension.dll' 'Install U13 deve apontar para a DLL satélite, não para a build canônica.'
Assert-Contains $u13InstallBat '-BuildDll "%BUILD_DLL%" -GeneXusDirectory "%GENEXUS_DIRECTORY%"' 'Install U13 deve copiar usando explicitamente a DLL satélite e o diretório escolhido.'
Assert-Contains $u13InstallBat '-BuildDll "%BUILD_DLL%" -InstalledDll' 'Install U13 deve validar o hash contra a mesma DLL satélite usada na cópia.'
Assert-Contains $u13InstallBat 'if exist "%GENEXUS_DIRECTORY%\GeneXus.exe" goto geneXusFound' 'Install U13 deve validar caminhos com (x86) sem bloco parentizado.'
Assert-Contains $installBat 'Register-ExtensionForGeneXus18.bat "%GENEXUS_DIRECTORY%"' 'Install canônico deve orientar o Register com o mesmo diretório da cópia.'
Assert-Contains $u13InstallBat 'Register-ExtensionForGeneXus18.bat "%GENEXUS_DIRECTORY%"' 'Install U13 deve orientar o Register com o mesmo diretório da cópia satélite.'
Assert-NotContains $installBat 'Register-ExtensionForGeneXus18.bat normalmente' 'Install canônico não pode omitir o diretório no eco do Register.'
Assert-NotContains $u13InstallBat 'Register-ExtensionForGeneXus18.bat normalmente' 'Install U13 não pode omitir o diretório no eco do Register.'

Write-Output 'PASS: InstallExtensionBatPathHandling'
