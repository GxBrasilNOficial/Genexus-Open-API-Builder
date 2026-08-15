#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$readerPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\PrototypeWizardContract.cs'
$existingReaderPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\PrototypeWizardExistingApiContractReader.cs'
$packagePath = Join-Path $PSScriptRoot '..\..\Src\Extension\Package.cs'
$dialogPath = Join-Path $PSScriptRoot '..\..\Src\Extension\PrototypeWizardDialog.cs'
foreach ($path in @($readerPath, $existingReaderPath, $packagePath, $dialogPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "SOURCE_MISSING: $path"
    }
}

function Assert-Contains {
    param([string]$Text, [string]$Needle, [string]$Message)
    if ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "ASSERT_CONTAINS_FAILED: $Message"
    }
}

$reader = [IO.File]::ReadAllText($readerPath)
$existingReader = [IO.File]::ReadAllText($existingReaderPath)
$package = [IO.File]::ReadAllText($packagePath)
$dialog = [IO.File]::ReadAllText($dialogPath)

Assert-Contains $reader 'Read(KBModel designModel, Transaction transaction)' 'O snapshot do Wizard deve aceitar a KB ativa para reencounter.'
Assert-Contains $reader 'existingApiContract.FiltersAvailable ? false : filter.DefaultSelected' 'Com contrato de filtros existente, campos novos nao devem voltar aos defaults de uma API nova.'
Assert-Contains $reader 'ResolveExistingFieldSelection' 'Create, Update e Response devem usar a seleção persistida quando disponível.'
Assert-Contains $reader 'TryGetServiceSelection' 'Os serviços existentes devem iniciar com a mesma seleção da API.'
Assert-Contains $reader 'existingFilter is not null' 'Filtros existentes devem iniciar marcados.'
Assert-Contains $existingReader 'API.GetAll(designModel)' 'A leitura inicial deve consultar o API Object existente.'
Assert-Contains $existingReader 'ServiceBlockPattern' 'A leitura deve localizar os serviços persistidos.'
Assert-Contains $existingReader 'fields.listFilters' 'A metadata deve ser fallback para os filtros persistidos.'
Assert-Contains $existingReader 'fields.createRequest' 'A metadata deve restaurar os campos do CreateRequest.'
Assert-Contains $existingReader 'fields.updateRequest' 'A metadata deve restaurar os campos do UpdateRequest.'
Assert-Contains $existingReader 'fields.response' 'A metadata deve restaurar os campos do Response.'
Assert-Contains $existingReader 'fields.required' 'A metadata deve restaurar os campos obrigatórios.'
Assert-Contains $existingReader 'pagination.defaultPageSize' 'A metadata deve restaurar a paginação.'
Assert-Contains $existingReader 'ReadStaticOrder' 'A metadata deve restaurar a ordenação.'
Assert-Contains $existingReader 'ApiPlanOwnedObjectDescription.IsOwnedMetadataFile' 'Somente metadata própria deve alimentar o reencounter.'
Assert-Contains $existingReader 'ReadOwnedSdtFields' 'SDTs próprios devem ser fallback quando a metadata não estiver disponível.'
Assert-Contains $existingReader 'metadata.Services.IsAvailable' 'A seleção de serviços deve preferir a metadata existente.'
Assert-Contains $reader 'ExistingApiContract' 'O snapshot deve transportar o contrato existente para as demais abas.'
Assert-Contains $package 'ApiPlanBuilder.Build(transaction, selection)' 'O fluxo principal continua montando o ApiPlan a partir das escolhas editáveis.'
Assert-Contains $package 'PrototypeWizardContractReader.Read(knowledgeBase.DesignModel, transaction)' 'O Wizard principal deve usar a leitura da KB ativa.'
Assert-Contains $package 'ReadForIntentionalChange(knowledgeBase.DesignModel, transaction, apiPlan)' 'O Wizard deve validar o baseline sem bloquear mudancas deliberadas no contrato.'
Assert-Contains $package 'ValidateForIntentionalChange(' 'O preflight do Wizard deve aceitar um novo plano depois de validar o baseline.'
Assert-Contains $package 'allowIntentionalContractRefresh: true' 'As escritas confirmadas pelo Wizard devem atualizar deliberadamente o contrato.'
Assert-Contains $dialog 'ReadForIntentionalChange(_designModel, _transaction' 'O estado exibido dentro do Wizard também deve aceitar mudanças deliberadas.'

Write-Output 'PASS: PrototypeWizardExistingApiFilters'
