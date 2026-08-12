@echo off
setlocal EnableExtensions

set "GENEXUS_DIRECTORY=C:\GeneXus\Gx18\U13"
set "SCRIPT=%~dp0Tools\Copy-ExtensionForGeneXus18.ps1"

if not exist "%GENEXUS_DIRECTORY%\GeneXus.exe" (
    echo ERRO: GeneXus.exe nao encontrado em %GENEXUS_DIRECTORY%
    pause
    exit /b 1
)

echo Compilando a extensao para GeneXus 18 U13...
pwsh.exe -NoProfile -File "%~dp0Tools\Build-ExtensionForGeneXus18U13.ps1" -GeneXusDirectory "%GENEXUS_DIRECTORY%"
if not "%ERRORLEVEL%"=="0" (
    echo ERRO: Falha ao compilar a extensao para U13.
    pause
    exit /b 1
)

echo.
echo Este arquivo deve ser iniciado como Administrador para copiar a DLL.
echo Feche completamente a IDE GeneXus antes de continuar.
echo.
echo Aguarde. Copiando a extensao para U13...
pwsh.exe -NoProfile -File "%SCRIPT%" -GeneXusDirectory "%GENEXUS_DIRECTORY%" -Apply
set "EXITCODE=%ERRORLEVEL%"

echo.
if not "%EXITCODE%"=="0" (
    echo A copia da extensao para U13 terminou com erro. Codigo de saida: %EXITCODE%
) else (
    echo A copia e a validacao da extensao para U13 terminaram sem erro.
)
echo Execute Register-ExtensionForGeneXus18U13.bat para registrar a extensao se necessario.
pause
exit /b %EXITCODE%