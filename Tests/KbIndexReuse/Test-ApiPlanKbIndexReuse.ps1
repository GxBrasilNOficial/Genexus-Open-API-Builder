#requires -Version 7.4

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# B082 Etapa 1A: o indice da KB e criado uma vez por operacao de escrita e
# propagado por parametro. Este lint e textual — a compilacao nao acusa
# overload orfao nem Create fora da lista permitida.

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$srcRoot = Join-Path $repositoryRoot 'Src\Extension'

function Read-LfText {
    param([string]$Path)
    return ([IO.File]::ReadAllText($Path)).Replace("`r`n", "`n").Replace("`r", "`n")
}

$apiObjectWriter = Read-LfText (Join-Path $srcRoot 'Diagnostics\ApiPlanApiObjectWriter.cs')
$sdtWriter = Read-LfText (Join-Path $srcRoot 'Diagnostics\ApiPlanSdtWriter.cs')
$listWriter = Read-LfText (Join-Path $srcRoot 'Diagnostics\ApiPlanListProcedureWriter.cs')
$bcWriter = Read-LfText (Join-Path $srcRoot 'Diagnostics\ApiPlanBusinessComponentWriter.cs')
$preflight = Read-LfText (Join-Path $srcRoot 'Diagnostics\ApiPlanWritePreflight.cs')
$stateReader = Read-LfText (Join-Path $srcRoot 'Diagnostics\ApiPlanGenerationStateReader.cs')
$procedureWriter = Read-LfText (Join-Path $srcRoot 'Diagnostics\ApiPlanProcedureWriter.cs')
$package = Read-LfText (Join-Path $srcRoot 'Package.cs')
$remover = Read-LfText (Join-Path $srcRoot 'Diagnostics\ApiPlanGeneratedApiRemover.cs')

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw "ASSERT_TRUE_FAILED: $Message"
    }
}

function Assert-Contains {
    param([string]$Text, [string]$Needle, [string]$Message)
    $normalized = $Needle.Replace("`r`n", "`n").Replace("`r", "`n")
    if ($Text.IndexOf($normalized, [StringComparison]::Ordinal) -lt 0) {
        throw "ASSERT_CONTAINS_FAILED: $Message"
    }
}

function Assert-NotContains {
    param([string]$Text, [string]$Needle, [string]$Message)
    $normalized = $Needle.Replace("`r`n", "`n").Replace("`r", "`n")
    if ($Text.IndexOf($normalized, [StringComparison]::Ordinal) -ge 0) {
        throw "ASSERT_NOT_CONTAINS_FAILED: $Message"
    }
}

function Assert-NotMatch {
    param([string]$Text, [string]$Pattern, [string]$Message)
    if ([regex]::IsMatch($Text, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)) {
        throw "ASSERT_NOT_MATCH_FAILED: $Message"
    }
}

# --- 1. Remocao efetiva das onze assinaturas orfas (destino 1) ---

Assert-NotMatch $apiObjectWriter 'public static ApiPlanApiObjectWriteResult CreateOrReencounter\(\s*KBModel designModel,\s*Transaction transaction,\s*ApiPlan apiPlan\)' 'CreateOrReencounter de 3 args do API Object nao pode voltar.'
Assert-NotMatch $sdtWriter 'public static ApiPlanSdtWriteResult CreateOrReencounter\(\s*KBModel designModel,\s*Transaction transaction,\s*ApiPlan apiPlan\)' 'CreateOrReencounter de 3 args do SdtWriter nao pode voltar.'
Assert-NotMatch $listWriter 'public static ApiPlanListProcedureWriteResult Apply\(\s*KBModel model,\s*Transaction transaction,\s*ApiPlan plan\)' 'Apply de 3 args do List nao pode voltar.'
Assert-NotMatch $listWriter 'public static ApiPlanListProcedureWriteResult Apply\(\s*KBModel model,\s*Transaction transaction,\s*ApiPlan plan,\s*bool allowIntentionalContractRefresh\)' 'Apply de 4 args do List nao pode voltar.'
Assert-NotMatch $bcWriter 'public static ApiPlanBusinessComponentWriteResult Apply\(\s*KBModel model,\s*Transaction transaction,\s*ApiPlan plan\)' 'Apply de 3 args do Business Component nao pode voltar.'
Assert-NotMatch $bcWriter 'public static ApiPlanBusinessComponentWriteResult Apply\(\s*KBModel model,\s*Transaction transaction,\s*ApiPlan plan,\s*bool allowIntentionalContractRefresh\)' 'Apply de 4 args do Business Component nao pode voltar.'
Assert-NotContains $preflight 'public static void Validate(' 'Validate publico do preflight foi removido.'
Assert-NotMatch $preflight 'public static void ValidateForIntentionalChange\(\s*KBModel designModel,\s*Transaction transaction,\s*ApiPlan apiPlan\)' 'ValidateForIntentionalChange de 3 args nao pode voltar.'
Assert-NotContains $stateReader 'public static ApiPlanGenerationState ReadForSync(' 'ReadForSync de 3 args foi removido.'
Assert-NotContains $preflight 'public static void Validate(' 'Validate de 7 args do preflight foi removido junto com o de 3.'
Assert-NotMatch $stateReader 'private static ApiPlanGenerationState Read\(KBModel designModel, Transaction transaction, ApiPlan apiPlan\)' 'Read de 3 args do leitor de estado foi removido.'

# --- 2. Propagacao efetiva (1A-i + 1A-ii) ---

Assert-Contains $apiObjectWriter 'bool allowIntentionalContractRefresh,
        ApiPlanKbObjectNameIndex kbIndex,' 'CreateOrReencounter completa do API Object deve exigir kbIndex.'
Assert-Contains $sdtWriter 'internal static void Preflight(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        ApiPlanKbObjectNameIndex kbIndex)' 'ApiPlanSdtWriter.Preflight deve exigir kbIndex.'
Assert-Contains $preflight 'public static void ValidateForSync(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        ApiPlanKbObjectNameIndex kbIndex)' 'ValidateForSync deve exigir kbIndex.'
Assert-Contains $preflight 'bool requireMetadataFile,
        ApiPlanKbObjectNameIndex kbIndex)' 'ValidateForIntentionalChange publico (7 args + indice) deve exigir kbIndex.'
Assert-Contains $package 'private static bool TryApplyList(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        string triggerSource,
        ApiPlanKbObjectNameIndex kbIndex,' 'Package.TryApplyList deve exigir kbIndex.'
Assert-Contains $package 'private static bool TryCreateSdts(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        string triggerSource,
        ApiPlanKbObjectNameIndex kbIndex,' 'Package.TryCreateSdts deve exigir kbIndex sem default nulo.'
Assert-Contains $package 'private static bool TryCreateProcedures(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        string triggerSource,
        ApiPlanKbObjectNameIndex kbIndex,' 'Package.TryCreateProcedures deve exigir kbIndex sem default nulo.'
Assert-Contains $package 'private static bool TryApplyBusinessComponent(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        string triggerSource,
        ApiPlanKbObjectNameIndex kbIndex,' 'Package.TryApplyBusinessComponent deve exigir kbIndex sem default nulo.'
Assert-NotContains $package 'ApiPlanKbObjectNameIndex? kbIndex = null' 'Package nao pode declarar kbIndex opcional nulo nos caminhos convertidos da 1A.'
Assert-NotContains $package 'ApiPlanSdtSpecifier' 'Especificacao sincrona de SDTs no meio do Apply nao faz parte da 1A.'
Assert-NotContains $sdtWriter 'SkipValidation' 'Save de SDT no Apply nao usa SkipValidation; o reencontro idempotente evita regravar.'
Assert-Contains $sdtWriter 'MatchesPlannedSdtStructure' 'Reencontro de SDT deve comparar a estrutura persistida antes de Save.'
Assert-Contains $sdtWriter 'canSkipRewrite' 'Reencontro de SDT deve pular Save quando preserve ou estrutura ja bate.'
Assert-Contains $sdtWriter 'ApiPlanSdtWriteStatus.Unchanged' 'Reencontro sem Save deve publicar Unchanged, nao Reencountered.'
Assert-Contains $package 'preserveSdtNames: ApiPlanSdtWriter.PlannedSdtNames(apiPlan)' 'Wizard deve preservar SDTs ja gravados nas fases Business Component e List.'
Assert-Contains $package 'private static bool TryCreateApiObject(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        string triggerSource,
        ApiPlanKbObjectNameIndex kbIndex,' 'Package.TryCreateApiObject deve exigir kbIndex.'
Assert-Contains $procedureWriter 'private static ApiPlanProcedurePreflightResult PreflightProcedures(
        KBModel designModel,
        IReadOnlyList<ApiPlanProcedureDefinition> definitions,
        ApiPlanKbObjectNameIndex kbIndex)' 'PreflightProcedures deve exigir kbIndex.'
Assert-Contains $apiObjectWriter 'private static IReadOnlyList<Guid> PreflightRequiredSdts(
        KBModel designModel,
        ApiPlan apiPlan,
        ApiPlanKbObjectNameIndex kbIndex)' 'PreflightRequiredSdts deve exigir kbIndex.'
Assert-Contains $bcWriter 'private static void EnsureSdts(KBModel model, ApiPlan plan, ApiPlanKbObjectNameIndex kbIndex)' 'EnsureSdts deve exigir kbIndex.'
Assert-Contains $preflight 'string operationCode,
        ApiPlanKbObjectNameIndex kbIndex)' 'ValidateForIntentionalChange privado deve exigir kbIndex.'

Assert-Contains $bcWriter 'EnsureAttributeExists(
        ApiPlanKbObjectNameIndex kbIndex,' 'EnsureAttributeExists do Business Component deve receber o indice.'
Assert-Contains $listWriter 'EnsureAttributeExists(
        ApiPlanKbObjectNameIndex kbIndex,' 'EnsureAttributeExists do List deve receber o indice.'
Assert-Contains $bcWriter 'kbIndex.TryGetSingleAttribute' 'Business Component deve resolver atributo pelo mapa do indice.'
Assert-Contains $listWriter 'kbIndex.TryGetSingleAttribute' 'List deve resolver atributo pelo mapa do indice.'
Assert-NotContains $bcWriter 'bc-find-attribute' 'Marca Attribute/bc-find-attribute deve desaparecer.'
Assert-NotContains $listWriter 'list-find-attribute' 'Marca Attribute/list-find-attribute deve desaparecer.'
Assert-Contains $bcWriter 'TrySetAttributeBasedOn(ApiPlanKbObjectNameIndex kbIndex,' 'TrySetAttributeBasedOn do Business Component deve receber o indice.'
Assert-Contains $listWriter 'TrySetAttributeBasedOn(ApiPlanKbObjectNameIndex kbIndex,' 'TrySetAttributeBasedOn do List deve receber o indice.'
Assert-Contains $bcWriter 'TrySetVariableType(KBModel model, ApiPlanKbObjectNameIndex kbIndex,' 'TrySetVariableType deve receber kbIndex alem de KBModel.'
Assert-Contains $bcWriter 'IsManagedApiObject(KBModel model, ApiPlanKbObjectNameIndex kbIndex,' 'IsManagedApiObject deve exigir kbIndex.'
Assert-Contains $listWriter 'IsB070ApiObject(KBModel model, ApiPlanKbObjectNameIndex kbIndex,' 'IsB070ApiObject deve exigir kbIndex.'

Assert-Contains $apiObjectWriter 'private static IReadOnlyList<ApiPlanApiObjectProcedureDependency> PreflightRequiredProcedures(KBModel designModel, ApiPlan apiPlan)' 'PreflightRequiredProcedures permanece em leitura corrente nesta fatia.'
Assert-Contains $bcWriter 'private static Procedure FindProcedure(KBModel model, ApiPlan plan, string service, string backlog)' 'FindProcedure permanece em leitura corrente nesta fatia.'
Assert-Contains $listWriter 'private static Procedure FindListProcedure(KBModel model, ApiPlan plan)' 'FindListProcedure permanece em leitura corrente nesta fatia.'
Assert-Contains $apiObjectWriter 'apiobject-preflight-procedure' 'Marca Procedure/apiobject-preflight-procedure deve permanecer.'
Assert-Contains $bcWriter 'bc-find-procedure' 'Marca Procedure/bc-find-procedure deve permanecer.'
Assert-Contains $listWriter 'list-find-procedure' 'Marca Procedure/list-find-procedure deve permanecer.'

# --- 3. Origem unica de ApiPlanKbObjectNameIndex.Create, por simbolo ---

$allowedCreateSymbols = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
[void]$allowedCreateSymbols.Add('ReadForIntentionalChangeWithIndex')
# Abertura do Wizard (D8): ReadForIntentionalChange -> Read privado de 4 args.
# Apply/Sync nao passam por este Create; o preflight usa ReadUsingExistingIndex.
[void]$allowedCreateSymbols.Add('Read')
[void]$allowedCreateSymbols.Add('ExecuteSynchronizeWithTransaction')
[void]$allowedCreateSymbols.Add('ExecuteRemoveGeneratedApi')
# Validacao agregada do Remover, antes de qualquer Delete (Nivel A).
[void]$allowedCreateSymbols.Add('Remove')

$methodDeclaration = [regex]::new('(?:public|internal|private)\s+static\s+[^{;=]+?\s+(\w+)\s*\(', [System.Text.RegularExpressions.RegexOptions]::Singleline)
$createCall = [regex]::new('ApiPlanKbObjectNameIndex\.Create\s*\(')
$csFiles = Get-ChildItem -LiteralPath $srcRoot -Filter '*.cs' -Recurse -File
foreach ($file in $csFiles) {
    $text = Read-LfText $file.FullName
    foreach ($call in $createCall.Matches($text)) {
        $before = $text.Substring(0, $call.Index)
        $declared = $methodDeclaration.Matches($before)
        Assert-True ($declared.Count -gt 0) "Create sem metodo envolvente em $($file.Name)."
        $symbol = $declared[$declared.Count - 1].Groups[1].Value
        Assert-True $allowedCreateSymbols.Contains($symbol) "Create fora da lista permitida: simbolo='$symbol' arquivo='$($file.Name)'."
    }
}

Assert-NotContains $preflight 'ReadForIntentionalChange' 'ApiPlanWritePreflight nao pode chamar ReadForIntentionalChange; usa o indice ja criado.'
Assert-NotContains $preflight 'ReadForSync' 'ApiPlanWritePreflight nao pode chamar ReadForSync; usa ReadUsingExistingIndex.'
Assert-Contains $preflight 'ReadUsingExistingIndex' 'Preflight agregado deve reler o estado no indice ja criado.'
Assert-Contains $remover 'ApiPlanKbObjectNameIndex.Create' 'Remove cria o indice da validacao agregada antes de qualquer Delete.'

Write-Output 'PASS: ApiPlanKbIndexReuse'
