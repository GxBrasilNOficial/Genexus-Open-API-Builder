@echo off
setlocal EnableExtensions

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

echo Este arquivo deve ser iniciado manualmente como Administrador.
echo Feche completamente a IDE GeneXus antes de continuar.
echo.
echo Aguarde. Copiando a extensao...
pwsh.exe -NoProfile -File "%SCRIPT%" -Apply
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" goto report

echo Validando se a DLL instalada corresponde a build atual...
pwsh.exe -NoProfile -File "%VERIFY_SCRIPT%"
set "EXITCODE=%ERRORLEVEL%"

:report
echo.
if not "%EXITCODE%"=="0" (
    echo A copia da extensao terminou com erro. Codigo de saida: %EXITCODE%
) else (
    echo A copia e a validacao da extensao terminaram sem erro de processo.
)
echo Execute Register-ExtensionForGeneXus18.bat normalmente para registrar a extensao.
pause
exit /b %EXITCODE%
