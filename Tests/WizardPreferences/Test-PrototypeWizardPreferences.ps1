Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$codecPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\PrototypeWizardPreferencesCodec.cs'
$newtonsoftPath = Get-ChildItem -Path (Join-Path $env:USERPROFILE '.nuget\packages\newtonsoft.json') -Filter Newtonsoft.Json.dll -Recurse |
    Where-Object { $_.FullName -match '\\lib\\netstandard2\.0\\Newtonsoft\.Json\.dll$' } |
    Sort-Object FullName -Descending |
    Select-Object -First 1 -ExpandProperty FullName

if ([string]::IsNullOrWhiteSpace($newtonsoftPath)) {
    throw 'Newtonsoft.Json.dll não encontrado no cache NuGet local.'
}

$runtimeAssemblies = @([System.AppContext]::GetData('TRUSTED_PLATFORM_ASSEMBLIES') -split [System.IO.Path]::PathSeparator |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($runtimeAssemblies.Count -eq 0) {
    throw 'Assemblies do runtime PowerShell atual não foram encontrados.'
}

Add-Type -Path $codecPath -ReferencedAssemblies @(($runtimeAssemblies + $newtonsoftPath) | Sort-Object -Unique)

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw "ASSERT_TRUE_FAILED: $Message"
    }
}

function Assert-False {
    param([bool]$Condition, [string]$Message)
    if ($Condition) {
        throw "ASSERT_FALSE_FAILED: $Message"
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)
    if ($Expected -ne $Actual) {
        throw "ASSERT_EQUAL_FAILED: $Message Expected='$Expected' Actual='$Actual'"
    }
}

function Assert-Throws {
    param([scriptblock]$Script, [string]$Message)
    $thrown = $false
    try {
        & $Script
    }
    catch {
        $thrown = $true
    }

    if (-not $thrown) {
        throw "ASSERT_THROWS_FAILED: $Message"
    }
}

$defaults = [GenexusOpenApiBuilder.Extension.Diagnostics.PrototypeWizardPreferencesCodec]::CreateDefault()
Assert-False $defaults.GenerateSdtsByDefault 'Default conservador não deve marcar SDTs.'
Assert-False $defaults.GenerateProceduresByDefault 'Default conservador não deve marcar Procedures.'
Assert-False $defaults.GenerateApiObjectByDefault 'Default conservador não deve marcar API Object.'
Assert-False $defaults.GenerateMetadataByDefault 'Default conservador não deve marcar Metadata.'
Assert-False $defaults.ApplyListByDefault 'Default conservador não deve marcar List.'
Assert-False $defaults.ApplyBusinessComponentByDefault 'Default conservador não deve marcar Business Component.'
Assert-True $defaults.ListServiceByDefault 'Serviço List deve iniciar habilitado.'
Assert-True $defaults.GetServiceByDefault 'Serviço Get deve iniciar habilitado.'
Assert-True $defaults.CreateServiceByDefault 'Serviço Create deve iniciar habilitado.'
Assert-True $defaults.UpdateServiceByDefault 'Serviço Update deve iniciar habilitado.'
Assert-Equal 'Authentication' $defaults.SecurityLevelByDefault 'Segurança default deve ser Authentication.'
Assert-Equal 50 $defaults.DefaultPageSizeByDefault 'Paginação default deve iniciar em 50.'
Assert-Equal 200 $defaults.MaximumPageSizeByDefault 'Paginação máxima default deve iniciar em 200.'

$values = [GenexusOpenApiBuilder.Extension.Diagnostics.PrototypeWizardPreferenceValues]::new()
$values.GenerateSdtsByDefault = $true
$values.GenerateProceduresByDefault = $true
$values.GenerateApiObjectByDefault = $false
$values.GenerateMetadataByDefault = $true
$values.ApplyListByDefault = $true
$values.ApplyBusinessComponentByDefault = $false
$values.ListServiceByDefault = $true
$values.GetServiceByDefault = $false
$values.CreateServiceByDefault = $true
$values.UpdateServiceByDefault = $false
$values.SecurityLevelByDefault = 'authorization'
$values.DefaultPageSizeByDefault = 40
$values.MaximumPageSizeByDefault = 100

$json = [GenexusOpenApiBuilder.Extension.Diagnostics.PrototypeWizardPreferencesCodec]::Serialize($values)
$parsed = [GenexusOpenApiBuilder.Extension.Diagnostics.PrototypeWizardPreferencesCodec]::Parse($json)

Assert-True $parsed.GenerateSdtsByDefault 'Serialização deve preservar GenerateSdts.'
Assert-True $parsed.GenerateProceduresByDefault 'Serialização deve preservar GenerateProcedures.'
Assert-False $parsed.GenerateApiObjectByDefault 'Serialização deve preservar GenerateApiObject.'
Assert-True $parsed.GenerateMetadataByDefault 'Serialização deve preservar GenerateMetadata.'
Assert-True $parsed.ApplyListByDefault 'Serialização deve preservar ApplyList.'
Assert-False $parsed.ApplyBusinessComponentByDefault 'Serialização deve preservar ApplyBusinessComponent.'
Assert-True $parsed.ListServiceByDefault 'Serialização deve preservar serviço List.'
Assert-False $parsed.GetServiceByDefault 'Serialização deve preservar serviço Get.'
Assert-True $parsed.CreateServiceByDefault 'Serialização deve preservar serviço Create.'
Assert-False $parsed.UpdateServiceByDefault 'Serialização deve preservar serviço Update.'
Assert-Equal 'Authorization' $parsed.SecurityLevelByDefault 'SecurityLevel deve ser normalizado ao serializar/parsear.'
Assert-Equal 40 $parsed.DefaultPageSizeByDefault 'Serialização deve preservar DefaultPageSize.'
Assert-Equal 100 $parsed.MaximumPageSizeByDefault 'Serialização deve preservar MaximumPageSize.'

$legacyJson = @'
{
  "schemaVersion": "GOAB_WIZARD_PREFERENCES_V1",
  "wizardDefaults": {
    "generateSdts": true,
    "generateProcedures": false,
    "generateApiObject": true,
    "generateMetadata": false,
    "applyList": true,
    "applyBusinessComponent": false
  }
}
'@
$legacyParsed = [GenexusOpenApiBuilder.Extension.Diagnostics.PrototypeWizardPreferencesCodec]::Parse($legacyJson)
Assert-True $legacyParsed.ListServiceByDefault 'JSON sem bloco services deve aplicar fallback conservador habilitado.'
Assert-Equal 'Authentication' $legacyParsed.SecurityLevelByDefault 'JSON sem securityLevel deve aplicar fallback Authentication.'
Assert-Equal 50 $legacyParsed.DefaultPageSizeByDefault 'JSON sem pagination deve aplicar DefaultPageSize fallback.'
Assert-Equal 200 $legacyParsed.MaximumPageSizeByDefault 'JSON sem pagination deve aplicar MaximumPageSize fallback.'

$invalidPagination = $json.Replace('"defaultPageSize": 40', '"defaultPageSize": 120')
Assert-Throws { [GenexusOpenApiBuilder.Extension.Diagnostics.PrototypeWizardPreferencesCodec]::Parse($invalidPagination) | Out-Null } 'DefaultPageSize maior que MaximumPageSize deve ser rejeitado.'

$invalidServices = $json.Replace('"list": true', '"list": false').Replace('"create": true', '"create": false')
Assert-Throws { [GenexusOpenApiBuilder.Extension.Diagnostics.PrototypeWizardPreferencesCodec]::Parse($invalidServices) | Out-Null } 'Todos os serviços desmarcados devem ser rejeitados.'

Assert-Equal 'None' ([GenexusOpenApiBuilder.Extension.Diagnostics.PrototypeWizardPreferencesCodec]::NormalizeSecurityLevel('none')) 'SecurityLevel None deve ser normalizado.'
Assert-Equal 'Authentication' ([GenexusOpenApiBuilder.Extension.Diagnostics.PrototypeWizardPreferencesCodec]::NormalizeSecurityLevel('valor-invalido')) 'SecurityLevel inválido deve cair em Authentication.'

Write-Output 'PASS: PrototypeWizardPreferencesCodec'
