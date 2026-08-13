@echo off
setlocal EnableExtensions

set "GENEXUS_DIRECTORY=%~1"
if "%GENEXUS_DIRECTORY%"=="" set "GENEXUS_DIRECTORY=C:\Program Files (x86)\GeneXus\GeneXus18up13"

set "BUILD_DLL=%~dp0artifacts\gx18u13\bin\Release\net471\GenexusOpenApiBuilder.Extension.dll"
set "SCRIPT=%~dp0Tools\Copy-ExtensionForGeneXus18.ps1"
set "VERIFY_SCRIPT=%~dp0Tools\Test-InstalledExtension.ps1"

if exist "%SCRIPT%" goto scriptFound
echo ERRO: script PowerShell de copia nao encontrado: %SCRIPT%
pause
exit /b 1

:scriptFound
if exist "%VERIFY_SCRIPT%" goto verifyScriptFound
echo ERRO: script PowerShell de verificacao nao encontrado: %VERIFY_SCRIPT%
pause
exit /b 1

:verifyScriptFound
if exist "%BUILD_DLL%" goto buildFound
echo ERRO: DLL satelite U13 nao encontrada: %BUILD_DLL%
echo Execute o build Release de Src\GenexusOpenApiBuilder.Gx18u13.sln antes de continuar.
pause
exit /b 1

:buildFound
if exist "%GENEXUS_DIRECTORY%\GeneXus.exe" goto geneXusFound
echo ERRO: GeneXus.exe nao encontrado em %GENEXUS_DIRECTORY%
echo Informe o diretorio correto como primeiro argumento do .bat.
pause
exit /b 1

:geneXusFound
echo Este arquivo deve ser iniciado manualmente como Administrador.
echo Feche completamente a IDE GeneXus antes de continuar.
echo Diretorio GeneXus: %GENEXUS_DIRECTORY%
echo DLL satelite U13: %BUILD_DLL%
echo.
echo Aguarde. Copiando a extensao satelite U13...
pwsh.exe -NoProfile -File "%SCRIPT%" -Apply -BuildDll "%BUILD_DLL%" -GeneXusDirectory "%GENEXUS_DIRECTORY%"
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" goto report

echo Validando se a DLL satelite U13 instalada corresponde a build atual...
pwsh.exe -NoProfile -File "%VERIFY_SCRIPT%" -BuildDll "%BUILD_DLL%" -InstalledDll "%GENEXUS_DIRECTORY%\Packages\GenexusOpenApiBuilder.Extension.dll"
set "EXITCODE=%ERRORLEVEL%"

:report
echo.
if not "%EXITCODE%"=="0" (
    echo A copia da extensao satelite terminou com erro. Codigo de saida: %EXITCODE%
) else (
    echo A copia e a validacao da extensao satelite terminaram sem erro de processo.
)
echo O registro da extensao nao foi executado. Se o manifesto ainda nao estiver registrado nesta IDE, use Register-ExtensionForGeneXus18.bat normalmente e depois genexus /install.
pause
exit /b %EXITCODE%
