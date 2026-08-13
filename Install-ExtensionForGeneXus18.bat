@echo off
setlocal EnableExtensions

set "GENEXUS_DIRECTORY=%~1"
if "%GENEXUS_DIRECTORY%"=="" set "GENEXUS_DIRECTORY=C:\Program Files (x86)\GeneXus\GeneXus18"

set "SCRIPT=%~dp0Tools\Copy-ExtensionForGeneXus18.ps1"
set "VERIFY_SCRIPT=%~dp0Tools\Test-InstalledExtension.ps1"

if not exist "%SCRIPT%" (
    echo ERRO: script PowerShell de copia nao encontrado: %SCRIPT%
    pause
    exit /b 1
)

if not exist "%VERIFY_SCRIPT%" (
    echo ERRO: script PowerShell de verificacao nao encontrado: %VERIFY_SCRIPT%
    pause
    exit /b 1
)

if exist "%GENEXUS_DIRECTORY%\GeneXus.exe" goto geneXusFound
echo ERRO: GeneXus.exe nao encontrado em %GENEXUS_DIRECTORY%
echo Informe o diretorio correto como primeiro argumento do .bat.
pause
exit /b 1

:geneXusFound

echo Este arquivo deve ser iniciado manualmente como Administrador.
echo Feche completamente a IDE GeneXus antes de continuar.
echo Diretorio GeneXus: %GENEXUS_DIRECTORY%
echo.
echo Aguarde. Copiando a extensao...
pwsh.exe -NoProfile -File "%SCRIPT%" -Apply -GeneXusDirectory "%GENEXUS_DIRECTORY%"
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" goto report

echo Validando se a DLL instalada corresponde a build atual...
pwsh.exe -NoProfile -File "%VERIFY_SCRIPT%" -InstalledDll "%GENEXUS_DIRECTORY%\Packages\GenexusOpenApiBuilder.Extension.dll"
set "EXITCODE=%ERRORLEVEL%"

:report
echo.
if not "%EXITCODE%"=="0" (
    echo A copia da extensao terminou com erro. Codigo de saida: %EXITCODE%
) else (
    echo A copia e a validacao da extensao terminaram sem erro de processo.
)
echo Execute Register-ExtensionForGeneXus18.bat "%GENEXUS_DIRECTORY%" somente se o manifesto, a identidade do pacote ou o registro de comandos mudou desde o ultimo genexus /install bem-sucedido.
echo Use cmd normal (sem Administrador). No prompt aberto, digite genexus /install e depois exit.
pause
exit /b %EXITCODE%
