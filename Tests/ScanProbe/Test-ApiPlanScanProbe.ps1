#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# B082 - contrato do probe de medicao de varreduras. Ele nao decide comportamento da
# extensao, mas produz os numeros que fundamentam o plano de hardening e desempenho:
# um vazamento de escopo atribuiria a uma operacao o custo de outra, em silencio.

$probePath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanScanProbe.cs'
$telemetryPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanScanTelemetry.cs'
Add-Type -Path @($probePath, $telemetryPath)

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw "ASSERT_TRUE_FAILED: $Message"
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)
    if ($Expected -ne $Actual) {
        throw "ASSERT_EQUAL_FAILED: $Message (expected='$Expected' actual='$Actual')"
    }
}

$probe = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanScanProbe]
$telemetryType = [GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanScanTelemetry]

# 1) Sem escopo ativo, Scan executa o delegate e nao contabiliza nada.
$semEscopo = $probe::Scan[int]('SDT', 'sem-escopo', [Func[int]] { 42 })
Assert-Equal 42 $semEscopo 'Scan sem escopo deve devolver o valor do delegate.'

# 2) Com escopo ativo, a varredura e contabilizada e o valor continua sendo devolvido.
$t1 = $telemetryType::new()
$scope1 = $probe::Begin($t1)
try {
    $comEscopo = $probe::Scan[string]('Procedure', 'com-escopo', [Func[string]] { 'ok' })
    Assert-Equal 'ok' $comEscopo 'Scan com escopo deve devolver o valor do delegate.'
    Assert-Equal 1 $t1.ScanCount 'Scan com escopo deve contabilizar uma varredura.'
}
finally {
    $scope1.Dispose()
}

# 3) Depois do Dispose, a medicao volta a ficar inativa.
[void]$probe::Scan[int]('SDT', 'apos-dispose', [Func[int]] { 1 })
Assert-Equal 1 $t1.ScanCount 'Varredura apos o Dispose nao pode ser atribuida ao escopo encerrado.'

# 4) O callback de encerramento roda exatamente uma vez, mesmo com Dispose repetido.
$script:callbackCount = 0
$t2 = $telemetryType::new()
$scope2 = $probe::Begin($t2, [Action[GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanScanTelemetry]] {
    param($telemetry)
    $script:callbackCount++
})
$scope2.Dispose()
$scope2.Dispose()
$scope2.Dispose()
Assert-Equal 1 $script:callbackCount 'O callback de encerramento deve rodar exatamente uma vez.'

# 5) Escopos aninhados restauram o anterior: o interno nao desliga a medicao do externo.
$externo = $telemetryType::new()
$interno = $telemetryType::new()
$scopeExterno = $probe::Begin($externo)
try {
    [void]$probe::Scan[int]('API', 'externo-antes', [Func[int]] { 1 })

    $scopeInterno = $probe::Begin($interno)
    try {
        [void]$probe::Scan[int]('API', 'interno', [Func[int]] { 1 })
    }
    finally {
        $scopeInterno.Dispose()
    }

    [void]$probe::Scan[int]('API', 'externo-depois', [Func[int]] { 1 })
}
finally {
    $scopeExterno.Dispose()
}

Assert-Equal 1 $interno.ScanCount 'O escopo interno deve receber apenas a sua propria varredura.'
Assert-Equal 2 $externo.ScanCount 'O escopo externo deve voltar a medir depois que o interno encerra.'

# 6) Suspend zera a medicao no trecho suspenso e restaura o escopo ao encerrar.
#    E o que impede a apresentacao do relatorio final de ser cobrada da operacao.
$comSuspensao = $telemetryType::new()
$scope3 = $probe::Begin($comSuspensao)
try {
    [void]$probe::Scan[int]('File', 'antes-da-suspensao', [Func[int]] { 1 })

    $suspensao = $probe::Suspend()
    try {
        $valorSuspenso = $probe::Scan[int]('File', 'durante-a-suspensao', [Func[int]] { 7 })
        Assert-Equal 7 $valorSuspenso 'Scan suspenso deve continuar executando o delegate.'
    }
    finally {
        $suspensao.Dispose()
    }

    [void]$probe::Scan[int]('File', 'depois-da-suspensao', [Func[int]] { 1 })
}
finally {
    $scope3.Dispose()
}

Assert-Equal 2 $comSuspensao.ScanCount 'A varredura feita durante a suspensao nao pode ser contabilizada.'

# 7) Exceção no callback nao pode derrubar o fluxo que estava sendo medido.
$t4 = $telemetryType::new()
$scope4 = $probe::Begin($t4, [Action[GenexusOpenApiBuilder.Extension.Diagnostics.ApiPlanScanTelemetry]] {
    param($telemetry)
    throw 'falha deliberada na publicacao'
})
$scope4.Dispose()

# 8) E o estado precisa ter sido restaurado mesmo com o callback falhando.
$aposFalha = $telemetryType::new()
$scope5 = $probe::Begin($aposFalha)
try {
    [void]$probe::Scan[int]('Folder', 'apos-callback-com-falha', [Func[int]] { 1 })
}
finally {
    $scope5.Dispose()
}

Assert-Equal 1 $aposFalha.ScanCount 'Uma falha no callback nao pode deixar a medicao inutilizada.'

# 9) A instancia inerte usada pelo Suspend nunca acumula medicao.
Assert-Equal 0 $telemetryType::Empty.ScanCount 'ApiPlanScanTelemetry.Empty deve permanecer sem varreduras.'

Write-Output 'PASS: ApiPlanScanProbe'
