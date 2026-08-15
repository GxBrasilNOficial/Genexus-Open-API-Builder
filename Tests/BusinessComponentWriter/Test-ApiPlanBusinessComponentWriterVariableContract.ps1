Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$writerPath = Join-Path $PSScriptRoot '..\..\Src\Extension\Diagnostics\ApiPlanBusinessComponentWriter.cs'
$apiPlanPath = Join-Path $PSScriptRoot '..\..\Src\Domain\ApiPlan.cs'
$source = Get-Content -Path $writerPath -Raw
$apiPlanSource = Get-Content -Path $apiPlanPath -Raw

function Assert-Contains {
    param([string]$Text, [string]$Expected, [string]$Message)
    if (-not $Text.Contains($Expected)) {
        throw "ASSERT_CONTAINS_FAILED: $Message Expected='$Expected'"
    }
}

function Assert-NotContains {
    param([string]$Text, [string]$Unexpected, [string]$Message)
    if ($Text.Contains($Unexpected)) {
        throw "ASSERT_NOT_CONTAINS_FAILED: $Message Unexpected='$Unexpected'"
    }
}

Assert-Contains $source 'new VariableSpec("ErrorResponse", "sdt_API_ErrorResponse")' 'Create/Update devem declarar ErrorResponse como corpo publico de erro.'
Assert-Contains $source 'new VariableSpec("HttpResponse", "HttpResponse")' 'Create deve declarar a variavel HttpResponse para emissao de cabecalhos HTTP.'
Assert-Contains $source 'new VariableSpec("LocationUrl", "VarChar(1K)")' 'Create deve declarar LocationUrl para montar o cabecalho Location em etapas.'
Assert-Contains $source '&HttpResponse.AddHeader(!\"Location\", &LocationUrl)' 'Create deve emitir Location a partir de LocationUrl ja montada.'
Assert-Contains $source 'CreateLocationUrlAssignments' 'Create deve montar LocationUrl segmento a segmento antes do AddHeader.'
Assert-Contains $source 'PrimaryKeyLocationPartExpression' 'CreateLocationUrlExpression deve delegar a formatacao da chave primaria a PrimaryKeyLocationPartExpression.'
Assert-Contains $source 'PadL(Trim(Str(Year({member}))), 4, !\"0\")' 'Campos de data na chave primaria devem ser formatados no padrao ISO YYYY-MM-DD no Location header.'
Assert-Contains $source 'URLEncode(Trim({member}))' 'Preflight deve preservar URLEncode(Trim(...)) como forma migravel anterior.'
Assert-Contains $source 'StrReplace(URLEncode(Trim({member})), !\"+\" , !\"%20\")' 'Campos de texto na chave primaria devem emitir path-encoding com espaco como %20.'
Assert-Contains $source 'PreviousB079CreateContentWithLocationUrlAndUrlEncodeTrim' 'Preflight deve migrar Create com LocationUrl e URLEncode(Trim(...)) sem StrReplace.'
Assert-Contains $source 'PreviousB079CreateContentWithoutLocationHeader' 'Preflight deve migrar Procedure Create gerada antes da inclusao do cabecalho Location.'
Assert-Contains $source 'PreviousB079CreateContentWithMethodTrimUrlEncodeLocationHeader' 'Preflight deve migrar Create com Location inline e URLEncode({member}.Trim()).'
Assert-Contains $source 'PreviousB079CreateContentWithInlineLocationHeader' 'Preflight deve migrar Create com Location inline em uma unica expressao.'
Assert-Contains $source 'StrSearch(Trim({bc}.{field.Name}), !\"/\") > 0' 'Create deve recusar chave de texto com barra antes do Commit.'
Assert-Contains $source 'Primary key text value must not contain' 'Create deve explicar a recusa de barra na chave de texto.'
Assert-Contains $source 'Rollback' 'Create deve desfazer o Save quando a chave de texto contem barra.'

$locationPartStart = $source.IndexOf('private static string PrimaryKeyLocationPartExpression', [StringComparison]::Ordinal)
$previousLocationPartStart = $source.IndexOf('private static string PreviousB079PrimaryKeyLocationPartExpressionWithUrlEncodeTrim', [StringComparison]::Ordinal)
if ($locationPartStart -lt 0 -or $previousLocationPartStart -lt 0 -or $previousLocationPartStart -le $locationPartStart) {
    throw 'ASSERT_SECTION_FAILED: nao foi possivel isolar PrimaryKeyLocationPartExpression atual.'
}

$currentLocationPartSource = $source.Substring($locationPartStart, $previousLocationPartStart - $locationPartStart)
Assert-Contains $currentLocationPartSource 'StrReplace(URLEncode(Trim({member})), !\"+\" , !\"%20\")' 'PrimaryKeyLocationPartExpression atual deve usar StrReplace sobre URLEncode(Trim(...)).'
Assert-NotContains $currentLocationPartSource 'return $"URLEncode(Trim({member}))"' 'PrimaryKeyLocationPartExpression atual nao deve retornar URLEncode(Trim(...)) sem StrReplace.'
Assert-NotContains $currentLocationPartSource 'URLEncode({member}.Trim())' 'PrimaryKeyLocationPartExpression atual nao deve usar .Trim() encadeado.'

$previousUrlEncodeTrimStart = $previousLocationPartStart
$previousMethodTrimStart = $source.IndexOf('private static string PreviousB079PrimaryKeyLocationPartExpressionWithMethodTrim', [StringComparison]::Ordinal)
if ($previousMethodTrimStart -lt 0 -or $previousMethodTrimStart -le $previousUrlEncodeTrimStart) {
    throw 'ASSERT_SECTION_FAILED: nao foi possivel isolar PreviousB079PrimaryKeyLocationPartExpressionWithUrlEncodeTrim.'
}

$previousUrlEncodeTrimSource = $source.Substring($previousUrlEncodeTrimStart, $previousMethodTrimStart - $previousUrlEncodeTrimStart)
Assert-Contains $previousUrlEncodeTrimSource 'URLEncode(Trim({member}))' 'Variante migravel deve preservar URLEncode(Trim(...)).'
Assert-NotContains $previousUrlEncodeTrimSource 'StrReplace(URLEncode' 'Variante migravel URLEncode(Trim) nao deve incluir StrReplace.'

Assert-Contains $source '[RestMethod({method.ToUpperInvariant()})]' 'API Object deve projetar RestMethod planejado, incluindo PUT no Update.'
Assert-Contains $source '[RestPath(\"{EscapeDescription(ResolveService(plan, service).RestPath.Trim())}\")]' 'API Object deve projetar RestPath planejado em cada servico REST.'
Assert-Contains $source '[SecurityLevel({plan.Security.SecurityLevel})]' 'API Object deve projetar SecurityLevel explicitamente em cada servico REST.'
Assert-Contains $source 'AddStatusEvent(events, "List")' 'API Object deve propagar o status HTTP também no retorno da List.'
Assert-Contains $source 'CreatePreviousB079ApiEvents' 'Preflight deve reconhecer Events anteriores sem List.After apenas para migração.'
Assert-Contains $apiPlanSource '"{&" + item.Name + "}"' 'ApiPlan deve gerar RestPath parametrizado com variavel GeneXus para o runtime casar path params.'
Assert-Contains $source 'MatchesPreviousB079RestMethodContract' 'Preflight deve reconhecer a versao B079 anterior apenas como migravel quando faltar PUT/RestPath.'
Assert-Contains $source '&ErrorResponse.Code = !\"validation_error\"' 'Procedure deve popular codigo de erro top-level para falha de regra de negocio.'
Assert-Contains $source 'private static IEnumerable<string> BusinessRuleFailureMessages' 'Preflight deve preservar o reconhecimento das falhas de regra geradas por versoes anteriores.'
Assert-Contains $source 'PreviousB079BusinessRuleFailureMessages' 'Preflight deve reconhecer a variante intermediaria com ErrorItem apenas para migracao.'
Assert-Contains $source 'lines.Add("    Commit")' 'Create deve confirmar a gravacao do Business Component antes de responder 201.'
Assert-Contains $source 'lines.Add("        Commit")' 'Update deve confirmar a gravacao do Business Component antes de responder 200.'
Assert-Contains $source 'PreviousB079CreateContentWithoutCommit' 'Preflight deve migrar Procedure Create intermediaria gerada sem Commit.'
Assert-Contains $source 'PreviousB079UpdateContentWithoutCommit' 'Preflight deve migrar Procedure Update intermediaria gerada sem Commit.'
Assert-Contains $source 'RequiredMemberPresenceValidation("UpdateRequest"' 'Update deve validar presenca de membros JSON obrigatorios antes do Save.'
Assert-Contains $source 'DefaultValueRequiredMemberValidation(requestName, requestVariable, requiredFields, spaces)' 'Validacao atual de obrigatorios deve comparar o membro recebido com o valor default do SDT.'
Assert-Contains $source 'FieldLimitValidation("&CreateRequest", plan.CreateRequestFields, 0)' 'Create deve validar os limites dos atributos antes do Save.'
Assert-Contains $source 'FieldLimitValidation("&UpdateRequest", plan.UpdateRequestFields, 0)' 'Update deve validar os limites dos atributos antes do Save.'
Assert-Contains $source 'NumericMaximumLiteral(field.Length, field.Decimals)' 'A validacao numerica deve derivar o maior valor a partir de Length e Decimals.'
Assert-Contains $source 'Len({member}) > {field.Length}' 'A validacao textual deve derivar o maior comprimento a partir de Length.'
Assert-Contains $source 'CreateContentWithoutFieldLimitValidation(plan)' 'Procedures Create antigas sem validacao de limite devem ser reconhecidas como migraveis.'
Assert-Contains $source 'UpdateContentWithoutFieldLimitValidation(plan)' 'Procedures Update antigas sem validacao de limite devem ser reconhecidas como migraveis.'
Assert-Contains $source 'If {requestVariable}.{field.Name} = {emptyVariable}.{field.Name}' 'Validacao atual deve comparar cada obrigatorio contra instancia vazia do proprio SDT de request, sem ramificar por tipo.'
Assert-Contains $source 'new VariableSpec(EmptyRequestVariableName(requestName), RequestSdtName(plan, requestName))' 'Procedures com obrigatorios devem declarar a instancia vazia do SDT de request usada na comparacao.'
Assert-Contains $source 'absentInPreviousVariants' 'Preflight nao pode exigir a instancia vazia do SDT em Procedures geradas antes desta validacao.'
Assert-Contains $source 'PreviousB079CreateContentWithNativeJsonValidation' 'Preflight deve migrar Create intermediario que tentava validar obrigatorios por HttpRequest e Properties.'
Assert-Contains $source 'PreviousB079CreateContentWithSdtDirtyValidation' 'Preflight deve migrar Create intermediario que consultava Dirty com nome interno do SDT.'
Assert-Contains $source 'PreviousB079SdtDirtyMemberPresenceValidation' 'Preflight deve preservar reconhecimento da versao intermediaria com comando csharp apenas para migracao.'
Assert-Contains $source '&RestStatusCode = 400' 'Payload sem membro JSON obrigatorio deve retornar erro de requisicao antes do BC Save.'
Assert-Contains $source 'new VariableSpec("RequestJsonHasRequiredMembers", "Boolean")' 'Procedures com obrigatorios devem declarar flag de validacao de presenca JSON.'
Assert-Contains $source 'MatchesVariableSetAllowingRequiredMemberMigrationVariables' 'Preflight deve migrar Procedures intermediarias com variaveis antigas de validacao por HttpRequest.'
Assert-Contains $source 'PreviousB079UpdateContentWithoutRequiredMemberValidation' 'Preflight deve migrar Update gerado antes da validacao de obrigatorios.'
Assert-Contains $source 'PreviousB079UpdateContentWithNewtonsoftRequiredMemberValidation' 'Preflight deve migrar Update intermediario gerado com Newtonsoft antes da troca para Regex.'
Assert-Contains $source 'PreviousB079UpdateContentWithWrappedRequiredMemberValidation' 'Preflight deve migrar Update intermediario que exigia wrapper UpdateRequest no corpo bruto.'
Assert-Contains $source 'PreviousB079UpdateContentWithUnwrappedRequiredMemberValidation' 'Preflight deve migrar Update intermediario que validava obrigatorios por Regex sem wrapper no corpo bruto.'
Assert-Contains $source 'PreviousB079UpdateContentWithOriginalMemberDirtyValidation' 'Preflight deve migrar Update intermediario que consultava Dirty com nome JSON publico em vez do nome interno do SDT.'

$currentPresenceStart = $source.IndexOf('private static IEnumerable<string> DefaultValueRequiredMemberValidation', [StringComparison]::Ordinal)
$previousDirtyStart = $source.IndexOf('private static IEnumerable<string> PreviousB079SdtDirtyMemberPresenceValidation', [StringComparison]::Ordinal)
if ($currentPresenceStart -lt 0 -or $previousDirtyStart -lt 0 -or $previousDirtyStart -le $currentPresenceStart) {
    throw 'ASSERT_SECTION_FAILED: nao foi possivel isolar DefaultValueRequiredMemberValidation atual.'
}

$currentPresenceSource = $source.Substring($currentPresenceStart, $previousDirtyStart - $currentPresenceStart)
Assert-NotContains $currentPresenceSource 'csharp ' 'Procedure gerada atualmente nao deve usar comando csharp para validar membros JSON obrigatorios.'
Assert-NotContains $currentPresenceSource '.IsDirty(' 'Procedure gerada atualmente nao deve chamar IsDirty: o metodo nao existe no Source GeneXus.'
Assert-NotContains $currentPresenceSource '&HttpRequest.ToString()' 'Procedure gerada atualmente nao deve tentar ler o corpo bruto: ele ja foi consumido pelo pipeline REST.'

$currentFailureStart = $source.IndexOf('private static IEnumerable<string> CurrentBusinessRuleFailureMessages', [StringComparison]::Ordinal)
$previousFailureStart = $source.IndexOf('private static IEnumerable<string> BusinessRuleFailureMessages', [StringComparison]::Ordinal)
if ($currentFailureStart -lt 0 -or $previousFailureStart -lt 0 -or $previousFailureStart -le $currentFailureStart) {
    throw 'ASSERT_SECTION_FAILED: nao foi possivel isolar CurrentBusinessRuleFailureMessages atual.'
}

$currentFailureSource = $source.Substring($currentFailureStart, $previousFailureStart - $currentFailureStart)
Assert-NotContains $currentFailureSource 'msg(' 'Procedure REST atual nao deve emitir msg em falha de regra de negocio.'
Assert-NotContains $currentFailureSource '&Messages = ' 'Procedure REST atual nao deve manter coleta de Messages sem consumidor.'
Assert-NotContains $currentFailureSource '&ErrorResponse.Errors.Add(&ErrorItem)' 'Procedure gerada atualmente nao deve chamar Errors.Add com item nested enquanto GeneXus rejeita a validacao do objeto.'

Write-Output 'PASS: ApiPlanBusinessComponentWriterVariableContract'
