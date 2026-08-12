#requires -Version 7.4
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Trava YAML versionado e Issue Forms do GitHub.
# Camada 1 (sempre): lint estrutural em PowerShell puro.
# Camada 2: parse real via python3 + pyyaml; ausência => exit 2 (environmentBlocked).
# Exit 0 = passed; 1 = failed; 2 = environmentBlocked.

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$acceptedBodyTypes = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($bodyType in @('markdown', 'input', 'textarea', 'dropdown', 'checkboxes')) {
    [void]$acceptedBodyTypes.Add($bodyType)
}

$failures = [System.Collections.Generic.List[string]]::new()
$environmentBlockReasons = [System.Collections.Generic.List[string]]::new()

function Add-Failure {
    param([string]$Message)
    $failures.Add($Message)
}

function Get-VersionedYamlRelativePaths {
    $listed = & git -C $repositoryRoot ls-files -- '*.yml' '*.yaml'
    if ($LASTEXITCODE -ne 0) {
        throw "Não foi possível listar YAML versionados (git ls-files exit=$LASTEXITCODE)."
    }

    $paths = [System.Collections.Generic.List[string]]::new()
    foreach ($relativePath in @($listed)) {
        if (-not [string]::IsNullOrWhiteSpace($relativePath)) {
            $paths.Add(($relativePath.Trim()).Replace('\', '/'))
        }
    }

    return @($paths | Sort-Object -Unique)
}

function Test-IsIssueFormPath {
    param([string]$RelativePath)
    if ($RelativePath -notmatch '(?i)^\.github/ISSUE_TEMPLATE/') {
        return $false
    }

    $leaf = [System.IO.Path]::GetFileName($RelativePath)
    return -not [string]::Equals($leaf, 'config.yml', [StringComparison]::OrdinalIgnoreCase)
}

function Get-UnquotedMappingValue {
    param([string]$Line)

    if ($Line -match '^\s*#') {
        return $null
    }

    # key: value  ou  - key: value  (item de lista com mapping inline)
    if ($Line -notmatch '^\s*(?:-\s+)?(?<key>[^:#\s][^:#]*?):\s*(?<value>.*)$') {
        return $null
    }

    return $Matches['value']
}

function Test-StructuralYamlFile {
    param(
        [string]$RelativePath,
        [string]$FullPath,
        [bool]$IsIssueForm
    )

    $rawBytes = [System.IO.File]::ReadAllBytes($FullPath)
    if ($rawBytes.Length -ge 3 -and $rawBytes[0] -eq 0xEF -and $rawBytes[1] -eq 0xBB -and $rawBytes[2] -eq 0xBF) {
        Add-Failure "${RelativePath}: arquivo YAML não deve ter BOM UTF-8."
    }

    $lines = [System.IO.File]::ReadAllLines($FullPath)
    $ids = [System.Collections.Generic.List[string]]::new()
    $hasName = $false
    $hasDescription = $false
    $hasBody = $false

    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        $lineNumber = $i + 1

        if ($line.Contains([char]9)) {
            Add-Failure "${RelativePath}:${lineNumber}: contém tabulação; use apenas espaços."
        }

        $value = Get-UnquotedMappingValue -Line $line
        if ($null -ne $value) {
            $trimmed = $value.TrimEnd()
            $isQuoted = $trimmed.StartsWith('"') -or $trimmed.StartsWith("'")
            $isBlockOrFlow = $trimmed.StartsWith('|') -or $trimmed.StartsWith('>') -or $trimmed.StartsWith('[') -or $trimmed.StartsWith('{')
            if (-not $isQuoted -and -not $isBlockOrFlow -and $trimmed.Contains(': ')) {
                Add-Failure "${RelativePath}:${lineNumber}: escalar não citado contém ': ' (dois-pontos seguido de espaço). Cite o valor ou reformule."
            }
        }

        if ($line -match '^(?<key>name|description|body)\s*:') {
            switch ($Matches['key']) {
                'name' { $hasName = $true }
                'description' { $hasDescription = $true }
                'body' { $hasBody = $true }
            }
        }

        if ($IsIssueForm -and $line -match '^\s*(?:-\s+)?type:\s*(?<type>\S+)\s*$') {
            $typeName = $Matches['type'].Trim('"', "'")
            if (-not $acceptedBodyTypes.Contains($typeName)) {
                Add-Failure "${RelativePath}:${lineNumber}: type '$typeName' fora do conjunto aceito pelo GitHub Issue Forms."
            }
        }

        if ($IsIssueForm -and $line -match '^\s+id:\s*(?<id>\S+)\s*$') {
            $idValue = $Matches['id'].Trim('"', "'")
            if ($ids.Contains($idValue)) {
                Add-Failure "${RelativePath}:${lineNumber}: id duplicado '$idValue'."
            }
            else {
                $ids.Add($idValue)
            }
        }
    }

    if ($IsIssueForm) {
        if (-not $hasName) {
            Add-Failure "${RelativePath}: formulário sem chave de topo 'name'."
        }
        if (-not $hasDescription) {
            Add-Failure "${RelativePath}: formulário sem chave de topo 'description'."
        }
        if (-not $hasBody) {
            Add-Failure "${RelativePath}: formulário sem chave de topo 'body'."
        }
    }
}

function Test-PythonYamlEnvironment {
    $python = Get-Command -Name 'python3' -ErrorAction SilentlyContinue
    if ($null -eq $python) {
        $environmentBlockReasons.Add('python3 não está disponível no PATH.')
        return $false
    }

    $probe = & python3 -c "import yaml; print(getattr(yaml, '__version__', 'ok'))" 2>&1
    if ($LASTEXITCODE -ne 0) {
        $detail = (($probe | Out-String).Trim())
        if ([string]::IsNullOrWhiteSpace($detail)) {
            $detail = "exit=$LASTEXITCODE"
        }
        $environmentBlockReasons.Add("pyyaml não está importável via python3 ($detail).")
        return $false
    }

    return $true
}

function Test-ParseYamlWithPyYaml {
    param(
        [string]$RelativePath,
        [string]$FullPath
    )

    $escaped = $FullPath.Replace('\', '\\').Replace('"', '\"')
    $code = "import sys, yaml; yaml.safe_load(open(r`"$escaped`", encoding='utf-8')); print('ok')"
    $parseOutput = & python3 -c $code 2>&1
    if ($LASTEXITCODE -ne 0) {
        $detail = (($parseOutput | Out-String).Trim())
        if ([string]::IsNullOrWhiteSpace($detail)) {
            $detail = "exit=$LASTEXITCODE"
        }
        Add-Failure "${RelativePath}: parse YAML (pyyaml) falhou: $detail"
    }
}

$yamlPaths = @(Get-VersionedYamlRelativePaths)
if ($yamlPaths.Count -eq 0) {
    Write-Output 'PASS: nenhum *.yml/*.yaml versionado para validar.'
    exit 0
}

foreach ($relativePath in $yamlPaths) {
    $fullPath = Join-Path $repositoryRoot ($relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        Add-Failure "${relativePath}: listado pelo git, mas ausente no disco."
        continue
    }

    $isIssueForm = Test-IsIssueFormPath -RelativePath $relativePath
    Test-StructuralYamlFile -RelativePath $relativePath -FullPath $fullPath -IsIssueForm $isIssueForm
}

$pythonReady = Test-PythonYamlEnvironment
if ($pythonReady) {
    foreach ($relativePath in $yamlPaths) {
        $fullPath = Join-Path $repositoryRoot ($relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
        if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
            Test-ParseYamlWithPyYaml -RelativePath $relativePath -FullPath $fullPath
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Output 'FAIL: validação de YAML / Issue Forms.'
    foreach ($item in $failures) {
        Write-Output " - $item"
    }
    if (-not $pythonReady) {
        Write-Output 'ENVIRONMENT: camada 2 não executada (ambiente incompleto), mas a camada 1 já falhou.'
        foreach ($item in $environmentBlockReasons) {
            Write-Output " - $item"
        }
    }
    exit 1
}

if (-not $pythonReady) {
    Write-Output 'ENVIRONMENT_BLOCKED: camada 1 ok, mas a camada 2 (python3 + pyyaml) não está disponível.'
    foreach ($item in $environmentBlockReasons) {
        Write-Output " - $item"
    }
    exit 2
}

Write-Output "PASS: $($yamlPaths.Count) YAML versionado(s) ok (lint estrutural + parse pyyaml)."
exit 0
