#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$readerPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\PrototypeWizardContract.cs'
$existingReaderPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\PrototypeWizardExistingApiContractReader.cs'
$packagePath = Join-Path $PSScriptRoot '..\..\Src\Extension\Package.cs'
$dialogPath = Join-Path $PSScriptRoot '..\..\Src\Extension\PrototypeWizardDialog.cs'
$apiPlanPath = Join-Path $PSScriptRoot '..\..\Src\Domain\ApiPlan.cs'
foreach ($path in @($readerPath, $existingReaderPath, $packagePath, $dialogPath, $apiPlanPath)) {
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
$apiPlan = [IO.File]::ReadAllText($apiPlanPath)

Assert-Contains $reader 'Read(KBModel designModel, Transaction transaction)' 'O snapshot do Wizard deve aceitar a KB ativa para reencounter.'
Assert-Contains $reader 'existingApiContract.FiltersAvailable ? false : filter.DefaultSelected' 'Com contrato de filtros existente, campos novos nao devem voltar aos defaults de uma API nova.'
Assert-Contains $reader 'ResolveExistingFieldSelection' 'Create, Update e Response devem usar a seleção persistida quando disponível.'
Assert-Contains $reader 'ResolveExistingServiceSelection' 'Os serviços existentes devem iniciar com a mesma seleção da API.'
Assert-Contains $reader 'if (existingApiContract.ServicesAvailable)' 'Com ServicesAvailable, serviço ausente do contrato persistido permanece desmarcado (nao fallback true).'
Assert-Contains $reader 'return !string.Equals(serviceName, "Delete", StringComparison.OrdinalIgnoreCase);' 'Sem contrato persistido, Delete inicia desmarcado; List/Get/Create/Update seguem o default ligado.'
Assert-Contains $reader 'TryGetServiceSelection' 'A resolução de serviço consulta a seleção persistida antes do fallback.'
Assert-Contains $reader 'existingFilter is not null' 'Filtros existentes devem iniciar marcados.'
Assert-Contains $existingReader 'API.GetAll(designModel)' 'A leitura inicial deve consultar o API Object existente.'
Assert-Contains $existingReader 'ResolveApiObject(designModel, transaction, metadata)' 'O reader deve resolver o API Object pelo contrato persistido, não apenas pelo nome convencional.'
Assert-Contains $existingReader 'ReadString(metadata.Document, "api.name")' 'Com metadata, o reader deve usar o ApiName customizado persistido.'
Assert-Contains $existingReader 'ReadString(metadata.Document, "ownership.apiName")' 'O ownership.apiName deve ser fallback do nome persistido.'
Assert-Contains $existingReader 'IsOwnedApiCandidateForTransaction' 'Sem metadata, o reader deve procurar conservadoramente um API Object customizado próprio da Transaction.'
Assert-Contains $existingReader 'ApiPlanOwnedObjectDescription.IsCanonical(api.Description, api.Name)' 'A descoberta sem metadata deve exigir Description canônica da extensão.'
Assert-Contains $existingReader 'GeneratedProcedureCallPattern.Matches(source)' 'A descoberta sem metadata deve exigir chamada a Procedure gerada para a Transaction.'
Assert-Contains $existingReader '.Concat(ownedMatches)' 'Sem metadata, o reader deve considerar em conjunto o nome convencional e APIs customizadas próprias.'
Assert-Contains $existingReader 'return candidates.Length == 1 ? candidates[0] : null;' 'API Objects candidatos ambíguos não devem ser escolhidos arbitrariamente.'
Assert-Contains $existingReader '(?<service>List|Get|Create|Update|Delete)' 'A regex de Service Source deve reconhecer o Delete opt-in além de List/Get/Create/Update.'
Assert-Contains $existingReader '_API_(?:List|Get|Create|Update|Delete)' 'A descoberta por Procedure gerada deve aceitar proc*_API_Delete.'
Assert-Contains $existingReader 'item["securityLevel"]?.Value<string>()' 'A metadata deve restaurar o SecurityLevel por serviço, inclusive o do Delete.'
Assert-Contains $existingReader '(?<![\w.])' 'A regex de Service Source deve ignorar chamadas como procX_API_List e Modulo.List.'
Assert-Contains $existingReader 'DuplicateServiceNames' 'Duplicidade real no Service Source deve ser reportada, nao convertida em excecao.'
Assert-Contains $existingReader 'fields.listFilters' 'A metadata deve ser fallback para os filtros persistidos.'
Assert-Contains $existingReader 'fields.createRequest' 'A metadata deve restaurar os campos do CreateRequest.'
Assert-Contains $existingReader 'fields.updateRequest' 'A metadata deve restaurar os campos do UpdateRequest.'
Assert-Contains $existingReader 'fields.response' 'A metadata deve restaurar os campos do Response.'
Assert-Contains $existingReader 'fields.required' 'A metadata deve restaurar os campos obrigatórios.'
Assert-Contains $existingReader 'pagination.defaultPageSize' 'A metadata deve restaurar a paginação.'
Assert-Contains $existingReader 'ReadStaticOrder' 'A metadata deve restaurar a ordenação.'
Assert-Contains $existingReader 'ApiPlanOwnedObjectDescription.IsOwnedMetadataFile' 'Somente metadata própria deve alimentar o reencounter.'
Assert-Contains $existingReader 'ReadOwnedSdtFields' 'SDTs próprios devem ser fallback quando a metadata não estiver disponível.'
Assert-Contains $existingReader 'foreach (SDTItem item in matches[0].SDTStructure.Root.Items)' 'A leitura de membros de SDT deve iterar StructureItemCollection sem cast genérico inválido.'
Assert-Contains $existingReader 'PersistedHierarchicalRoot' 'Contrato existente deve expor levels persistidos para o Wizard.'
Assert-Contains $existingReader 'ApiPlanMetadataLevelsCodec.TryReadRoot' 'Reader deve reler levels V2 da metadata.'
Assert-Contains $dialog 'ApplyPersistedPrune' 'Wizard deve restaurar seleção hierárquica no reencontro.'
Assert-Contains $existingReader 'metadata.Services.IsAvailable' 'A seleção de serviços deve preferir a metadata existente.'
Assert-Contains $reader 'ExistingApiContract' 'O snapshot deve transportar o contrato existente para as demais abas.'
Assert-Contains $package 'ApiPlanBuilder.Build(knowledgeBase.DesignModel, transaction, selection)' 'O fluxo principal continua montando o ApiPlan a partir das escolhas editáveis.'
Assert-Contains $package 'ApiPlanBuilder.Build(transaction, selection)' 'O fluxo de sincronização continua montando o ApiPlan a partir das escolhas editáveis.'
Assert-Contains $package 'PrototypeWizardContractReader.Read(knowledgeBase.DesignModel, transaction)' 'O Wizard principal deve usar a leitura da KB ativa.'
Assert-Contains $package 'ReadForIntentionalChange(knowledgeBase.DesignModel, transaction, apiPlan)' 'O Wizard deve validar o baseline sem bloquear mudancas deliberadas no contrato.'
Assert-Contains $package 'ValidateForIntentionalChange(' 'O preflight do Wizard deve aceitar um novo plano depois de validar o baseline.'
Assert-Contains $package 'allowIntentionalContractRefresh: true' 'As escritas confirmadas pelo Wizard devem atualizar deliberadamente o contrato.'
Assert-Contains $dialog 'ReadForIntentionalChange(_designModel, _transaction' 'O estado exibido dentro do Wizard também deve aceitar mudanças deliberadas.'
Assert-Contains $dialog 'HasGetCreateUpdateServices' 'A etapa Get/Create/Update REST só fica disponível com os três serviços selecionados.'
Assert-Contains $dialog 'Bloqueado: marque Get, Create e Update nos Serviços' 'Sem Create/Update o Wizard deve bloquear a etapa BC com motivo explícito.'
Assert-Contains $dialog 'WireServiceSelectionRefresh' 'Mudança de serviços deve recalcular a disponibilidade da etapa BC.'
Assert-Contains $apiPlan 'string.Equals(service, "Delete", StringComparison.OrdinalIgnoreCase)' 'No reencontro, o SecurityLevel do Delete deve seguir a escolha do Wizard, nao o valor vazio da metadata antiga.'
Assert-Contains $apiPlan '&& !string.IsNullOrWhiteSpace(deleteSecurityLevel)' 'O combo do Delete so prevalece quando o review trouxe um nivel explicito.'

Write-Output 'PASS: PrototypeWizardExistingApiFilters'
