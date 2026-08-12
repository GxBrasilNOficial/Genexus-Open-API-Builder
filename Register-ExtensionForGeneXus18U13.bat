@echo off
setlocal EnableExtensions

set "GENEXUS_DIRECTORY=C:\GeneXus\Gx18\U13"

if exist "%GENEXUS_DIRECTORY%\GeneXus.exe" goto geneXusFound
echo ERRO: GeneXus.exe nao encontrado em %GENEXUS_DIRECTORY%
pause
exit /b 1

:geneXusFound

pushd "%GENEXUS_DIRECTORY%"
echo.
echo Executando varredura do GeneXus 18 U13 (genexus.com /install)...
echo Acompanhe a varredura abaixo:
echo ----------------------------------------------------

"C:\GeneXus\Gx18\U13\genexus.com" /install

popd
echo.
echo ----------------------------------------------------
echo Varredura concluida!
pause
exit /b 0