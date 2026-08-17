#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw "ASSERT_TRUE_FAILED: $Message"
    }
}

function Assert-Contains {
    param([string]$Text, [string]$Expected, [string]$Message)
    if (-not $Text.Contains($Expected)) {
        throw "ASSERT_CONTAINS_FAILED: $Message"
    }
}

$readerPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\PrototypeWizardExistingApiContractReader.cs'
$packagePath = Join-Path $PSScriptRoot '..\..\Src\Extension\Package.cs'
$localizationPath = Join-Path $PSScriptRoot '..\..\Src\Domain\ExtensionOutputLocalization.cs'

Assert-True (Test-Path -LiteralPath $readerPath) 'O leitor do contrato de API existente deve estar presente.'
Assert-True (Test-Path -LiteralPath $packagePath) 'O Package.cs deve estar presente.'
Assert-True (Test-Path -LiteralPath $localizationPath) 'O catálogo de localização do Output deve estar presente.'

$readerText = Get-Content -LiteralPath $readerPath -Raw
$packageText = Get-Content -LiteralPath $packagePath -Raw
$localizationText = Get-Content -LiteralPath $localizationPath -Raw

# A regex real do leitor é extraída do próprio código para o teste exercitar o mesmo padrão de runtime.
$patternMatch = [regex]::Match($readerText, 'ServiceBlockPattern = new\(\s*@"(?<pattern>[^"]*)"')
Assert-True ($patternMatch.Success) 'O teste deve localizar o literal da regex ServiceBlockPattern no leitor.'

$serviceBlockPattern = $patternMatch.Groups['pattern'].Value
Assert-Contains $serviceBlockPattern '(?<![\w.])' 'A regex de declaração de serviço deve excluir nomes precedidos por identificador ou ponto.'

$serviceBlock = [regex]::new($serviceBlockPattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

# Service Source equivalente ao gerado para uma API parcial com apenas List e Get.
$partialSource = @'
    [Description("Lista NotaFiscal")]
    [RestPath("/notafiscal")]
    [SecurityLevel(None)]
    List(in: &ApiPage, in: &ApiPageSize, in: &NotaFiscalSerie, out: &ListResponse)
        => procNotaFiscal_API_List(&ApiPage, &ApiPageSize, &NotaFiscalSerie, &ListResponse);

    [Description("Obtem NotaFiscal")]
    [RestPath("/notafiscal/{&NotaFiscalId}")]
    [SecurityLevel(None)]
    Get(in: &NotaFiscalId, out: &GetResponse)
        => procNotaFiscal_API_Get(&NotaFiscalId, &GetResponse, &ErrorResponse, &RestStatusCode);
'@

$partialMatches = @($serviceBlock.Matches($partialSource))
Assert-True ($partialMatches.Count -eq 2) "O Source parcial deve produzir exatamente 2 declarações; obtido $($partialMatches.Count)."

$partialNames = @($partialMatches | ForEach-Object { $_.Groups['service'].Value })
Assert-True (@($partialNames | Where-Object { $_ -eq 'List' }).Count -eq 1) 'List deve aparecer uma única vez, sem contar a chamada procNotaFiscal_API_List.'
Assert-True (@($partialNames | Where-Object { $_ -eq 'Get' }).Count -eq 1) 'Get deve aparecer uma única vez, sem contar a chamada procNotaFiscal_API_Get.'
Assert-True (@($partialNames | Where-Object { $_ -eq 'Create' }).Count -eq 0) 'Create não deve ser reconhecido em um Source que não o declara.'
Assert-True (@($partialNames | Where-Object { $_ -eq 'Update' }).Count -eq 0) 'Update não deve ser reconhecido em um Source que não o declara.'

# Chamada qualificada por módulo também é chamada, não declaração.
$callOnlySource = @'
        => Modulo.List(&ApiPage, &ListResponse);
        => Modulo.procNotaFiscal_API_Create(&CreateRequest, &CreateResponse);
'@

$callOnlyMatches = @($serviceBlock.Matches($callOnlySource))
Assert-True ($callOnlyMatches.Count -eq 0) "Chamadas qualificadas não devem virar declaração; obtido $($callOnlyMatches.Count)."

# Source completo continua sendo lido por inteiro.
$fullSource = @'
    [RestPath("/notafiscal")]
    List(in: &ApiPage, out: &ListResponse)
        => procNotaFiscal_API_List(&ApiPage, &ListResponse);

    [RestPath("/notafiscal/{&NotaFiscalId}")]
    Get(in: &NotaFiscalId, out: &GetResponse)
        => procNotaFiscal_API_Get(&NotaFiscalId, &GetResponse);

    [RestMethod(POST)]
    [RestPath("/notafiscal")]
    Create(in: &CreateRequest, out: &CreateResponse)
        => procNotaFiscal_API_Create(&CreateRequest, &CreateResponse);

    [RestMethod(PUT)]
    [RestPath("/notafiscal/{&NotaFiscalId}")]
    Update(in: &NotaFiscalId, in: &UpdateRequest, out: &UpdateResponse)
        => procNotaFiscal_API_Update(&NotaFiscalId, &UpdateRequest, &UpdateResponse);
'@

$fullMatches = @($serviceBlock.Matches($fullSource))
Assert-True ($fullMatches.Count -eq 4) "O Source completo deve produzir exatamente 4 declarações; obtido $($fullMatches.Count)."

$fullNames = @($fullMatches | ForEach-Object { $_.Groups['service'].Value })
foreach ($expected in @('List', 'Get', 'Create', 'Update')) {
    Assert-True (@($fullNames | Where-Object { $_ -eq $expected }).Count -eq 1) "$expected deve aparecer uma única vez no Source completo."
}

# Duplicidade real do Source é tolerada pelo leitor e reportada, não convertida em exceção.
Assert-Contains $readerText 'declaredServiceNames' 'O leitor deve rastrear os nomes de serviço já declarados.'
Assert-Contains $readerText 'duplicateServiceNames' 'O leitor deve acumular os nomes de serviço duplicados.'
Assert-Contains $readerText 'public IReadOnlyList<string> DuplicateServiceNames { get; }' 'O contrato existente deve expor os serviços duplicados.'
Assert-Contains $readerText 'var distinctServices = (services ?? Array.Empty<PrototypeWizardExistingService>())' 'O contrato existente deve deduplicar os serviços recebidos antes de montar o dicionário.'
Assert-Contains $readerText '.GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)' 'A deduplicação de serviços e filtros deve ser case-insensitive.'
Assert-Contains $readerText 'group => group.First(), StringComparer.OrdinalIgnoreCase)' 'A primeira ocorrência de cada filtro deve vencer.'

Assert-Contains $packageText 'snapshot.ExistingApiContract.DuplicateServiceNames' 'A abertura do wizard deve consultar os serviços duplicados.'
Assert-Contains $packageText 'Service Source do API Object declara servico duplicado:' 'A abertura do wizard deve emitir o diagnóstico de duplicidade no Output.'

Assert-Contains $localizationText 'The API Object Service Source declares a duplicated service:' 'O diagnóstico de duplicidade deve ter tradução para inglês.'
Assert-Contains $localizationText 'El Service Source del API Object declara un servicio duplicado:' 'O diagnóstico de duplicidade deve ter tradução para espanhol.'

Write-Output 'OK: parser de Service Source do wizard reconhece apenas declarações e tolera duplicidade.'
