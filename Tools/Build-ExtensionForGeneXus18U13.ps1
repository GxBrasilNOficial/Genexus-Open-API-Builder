[CmdletBinding()]
param(
    [string]$GeneXusDirectory = 'C:\GeneXus\Gx18\U13',
    [string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$csprojPath = Join-Path $projectRoot 'Src\Extension\GenexusOpenApiBuilder.Extension.csproj'

if (-not (Test-Path -LiteralPath $GeneXusDirectory -PathType Container)) {
    throw "Diretório do GeneXus U13 não encontrado em: $GeneXusDirectory"
}

Write-Host "Iniciando compilação da extensão para GeneXus 18 U13..." -ForegroundColor Cyan
Write-Host "GeneXus Directory: $GeneXusDirectory"
Write-Host "Configuration:    $Configuration"

& dotnet build "$csprojPath" -c "$Configuration" -p:TargetGX=GX18U13 "-p:GX18U13Path=$GeneXusDirectory"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nCompilação concluída com sucesso para o GeneXus 18 U13!" -ForegroundColor Green
    $outputDll = Join-Path $projectRoot "Src\Extension\bin\$Configuration\net471\GenexusOpenApiBuilder.Extension.dll"
    if (Test-Path $outputDll) {
        $info = Get-Item $outputDll
        Write-Host "DLL gerada: $($info.FullName) ($($info.Length) bytes)"
    }
} else {
    throw "Falha na compilação da extensão para o GeneXus 18 U13."
}