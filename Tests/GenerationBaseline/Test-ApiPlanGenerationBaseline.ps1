#requires -Version 7.4

[CmdletBinding()]
param(
    [switch]$UpdateBaselines,
    [string]$DllPath = '',
    [string]$GeneXusDirectory = 'C:\Program Files (x86)\GeneXus\GeneXus18'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$baselinesRoot = Join-Path $PSScriptRoot 'Baselines'
if ([string]::IsNullOrWhiteSpace($DllPath)) {
    $DllPath = Join-Path $repositoryRoot 'Src\Extension\bin\Release\net471\GenexusOpenApiBuilder.Extension.dll'
}

function Write-Utf8LfFile {
    param(
        [Parameter(Mandatory)] [string]$Path,
        [Parameter(Mandatory)] [string]$Content
    )

    $directory = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
        [void][System.IO.Directory]::CreateDirectory($directory)
    }

    $normalized = $Content.Replace("`r`n", "`n").Replace("`r", "`n")
    if (-not $normalized.EndsWith("`n")) {
        $normalized += "`n"
    }

    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $normalized, $encoding)
}

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw "ASSERT_TRUE_FAILED: $Message"
    }
}

function Get-AssemblyDirectoryCandidates {
    param([string]$GeneXusRoot)

    $candidates = [System.Collections.Generic.List[string]]::new()
    foreach ($relative in @('Packages', 'GeneXusBlazorControls', '')) {
        $path = if ([string]::IsNullOrWhiteSpace($relative)) { $GeneXusRoot } else { Join-Path $GeneXusRoot $relative }
        if (Test-Path -LiteralPath $path -PathType Container) {
            $candidates.Add($path)
        }
    }

    $dllDirectory = Split-Path -Parent $DllPath
    if (Test-Path -LiteralPath $dllDirectory -PathType Container) {
        $candidates.Add($dllDirectory)
    }

    return @($candidates | Select-Object -Unique)
}

function Initialize-GeneXusAssemblyResolver {
    param([string[]]$SearchDirectories)

    $script:AssemblySearchDirectories = $SearchDirectories
    $script:AssemblyResolveHandler = [System.ResolveEventHandler]{
        param($sender, $args)
        $requestedName = New-Object System.Reflection.AssemblyName($args.Name)
        $simpleName = $requestedName.Name
        foreach ($directory in $script:AssemblySearchDirectories) {
            $candidate = Join-Path $directory ($simpleName + '.dll')
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                return [System.Reflection.Assembly]::LoadFrom($candidate)
            }
        }

        return $null
    }

    [System.AppDomain]::CurrentDomain.add_AssemblyResolve($script:AssemblyResolveHandler)
}

if (-not (Test-Path -LiteralPath $DllPath -PathType Leaf)) {
    Write-Error "DLL Release não encontrada: $DllPath. Compile Src/GenexusOpenApiBuilder.sln em Release antes deste teste."
    exit 2
}

if (-not (Test-Path -LiteralPath $GeneXusDirectory -PathType Container)) {
    Write-Error "Instalação GeneXus não encontrada em modo leitura: $GeneXusDirectory"
    exit 2
}

$searchDirectories = Get-AssemblyDirectoryCandidates -GeneXusRoot $GeneXusDirectory
Initialize-GeneXusAssemblyResolver -SearchDirectories $searchDirectories

try {
    $assembly = [System.Reflection.Assembly]::LoadFrom($DllPath)
    $baselineType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanGenerationBaseline', $true, $false)
    $fixtureType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanGenerationBaselineFixture', $true, $false)
    $snapshotType = $assembly.GetType('GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanGenerationBaselineSnapshot', $true, $false)

    $createFixtures = $baselineType.GetMethod('CreateFixtures', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $capture = $baselineType.GetMethod('Capture', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $normalize = $baselineType.GetMethod('NormalizeForComparison', [System.Reflection.BindingFlags]'Static, NonPublic, Public')
    $toFileMap = $snapshotType.GetMethod('ToFileMap', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $nameProperty = $fixtureType.GetProperty('Name', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')
    $planProperty = $fixtureType.GetProperty('Plan', [System.Reflection.BindingFlags]'Instance, NonPublic, Public')

    Assert-True ($null -ne $createFixtures) 'CreateFixtures não encontrado.'
    Assert-True ($null -ne $capture) 'Capture não encontrado.'
    Assert-True ($null -ne $normalize) 'NormalizeForComparison não encontrado.'
    Assert-True ($null -ne $toFileMap) 'ToFileMap não encontrado.'

    $fixtures = @($createFixtures.Invoke($null, @()))
    Assert-True ($fixtures.Count -ge 3) "Esperava pelo menos 3 fixtures; encontrado $($fixtures.Count)."

    $expectedNames = @('FlatSimpleKey', 'FlatCompositeKey', 'FlatNoAccept')
    $actualNames = @($fixtures | ForEach-Object { $nameProperty.GetValue($_) })
    foreach ($expectedName in $expectedNames) {
        Assert-True ($actualNames -contains $expectedName) "Fixture ausente: $expectedName"
    }

    $divergences = [System.Collections.Generic.List[string]]::new()
    foreach ($fixture in $fixtures) {
        $fixtureName = [string]$nameProperty.GetValue($fixture)
        $plan = $planProperty.GetValue($fixture)
        $snapshot = $capture.Invoke($null, @($plan))
        $fileMap = $toFileMap.Invoke($snapshot, @())
        $fixtureDirectory = Join-Path $baselinesRoot $fixtureName

        if ($UpdateBaselines) {
            foreach ($entry in $fileMap.GetEnumerator()) {
                $target = Join-Path $fixtureDirectory $entry.Key
                $content = [string]$normalize.Invoke($null, @([string]$entry.Value))
                Write-Utf8LfFile -Path $target -Content $content
            }

            continue
        }

        if (-not (Test-Path -LiteralPath $fixtureDirectory -PathType Container)) {
            $divergences.Add("Diretório de referência ausente: $fixtureDirectory")
            continue
        }

        foreach ($entry in $fileMap.GetEnumerator()) {
            $referencePath = Join-Path $fixtureDirectory $entry.Key
            if (-not (Test-Path -LiteralPath $referencePath -PathType Leaf)) {
                $divergences.Add("Arquivo de referência ausente: $referencePath")
                continue
            }

            $actual = [string]$normalize.Invoke($null, @([string]$entry.Value))
            if (-not $actual.EndsWith("`n")) {
                $actual += "`n"
            }

            $expected = [System.IO.File]::ReadAllText($referencePath).Replace("`r`n", "`n").Replace("`r", "`n")
            if (-not $expected.EndsWith("`n")) {
                $expected += "`n"
            }

            if ($actual -cne $expected) {
                $divergences.Add("Divergência em $fixtureName/$($entry.Key)")
            }
        }
    }

    if ($UpdateBaselines) {
        Write-Output "UPDATED: linha de base offline gravada em $baselinesRoot"
        Write-Output 'Recaptura deve ir em commit isolado, só com arquivos de referência e justificativa escrita.'
        exit 0
    }

    if ($divergences.Count -gt 0) {
        throw ("ASSERT_BASELINE_FAILED:`n" + ($divergences -join "`n"))
    }

    Write-Output 'PASS: ApiPlanGenerationBaseline'
}
finally {
    if ($null -ne $script:AssemblyResolveHandler) {
        [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($script:AssemblyResolveHandler)
    }
}
