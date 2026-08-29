@echo off
setlocal EnableExtensions

set "GENEXUS_DIRECTORY=%~1"
if "%GENEXUS_DIRECTORY%"=="" set "GENEXUS_DIRECTORY=C:\Program Files (x86)\GeneXus\GeneXus18up13"

if exist "%GENEXUS_DIRECTORY%\GeneXus.exe" goto geneXusFound
echo ERRO: GeneXus.exe nao encontrado em %GENEXUS_DIRECTORY%
echo Informe o diretorio correto como primeiro argumento do .bat.
pause
exit /b 1

:geneXusFound

powershell.exe -NoProfile -Command "$identity = [Security.Principal.WindowsIdentity]::GetCurrent(); $principal = [Security.Principal.WindowsPrincipal]::new($identity); if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { exit 1 }"
if not "%ERRORLEVEL%"=="0" (
    echo ERRO: execute este arquivo em um cmd normal, sem Administrador.
    pause
    exit /b 1
)

pushd "%GENEXUS_DIRECTORY%"
echo.
echo Prompt normal preparado para registrar a extensao satelite U13.
echo Diretorio GeneXus: %GENEXUS_DIRECTORY%
echo.
echo Digite:
echo   genexus /install
echo.
echo Depois de conferir a varredura, digite exit.
echo.
%ComSpec% /d /k
set "EXITCODE=%ERRORLEVEL%"
popd
exit /b %EXITCODE%
