#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Artech.Architecture.Common.Objects;
using Artech.Common;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Wiki;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Recuperação de um API Object próprio que ficou sem o File de metadata depois de uma
/// aplicação parcial — o caso `B115`.
///
/// **O que ela grava:** ownership e o inventário de objetos verificado na KB, que é tudo o
/// que <c>ApiPlanGeneratedApiRemovalPlan.FromMetadata</c> consome. Devolve ao usuário a
/// capacidade de remover a API gerada.
///
/// **O que ela não grava, de propósito:** `fields`, `pagination`, `order`, `services`,
/// `levels` e `transactionStructure`. Esses dados existem somente na metadata original e não
/// são reconstruíveis a partir da KB — inventá-los faria o Sincronizar comparar a API real
/// contra uma descrição falsa. As chaves ficam **ausentes**, não vazias, porque o leitor de
/// contrato existente só cai no fallback da KB quando a chave falta.
///
/// A metadata resultante carrega a marca <c>recovery.imported</c>, que libera o Remover e
/// bloqueia o Sincronizar até um Apply completo reescrevê-la.
///
/// Desenho e pontos em aberto registrados na seção 13 de
/// `Docs/Implementation/2026-09-04-B111-F3-PLANO-DURABILIDADE-E-REMOCAO.md`.
/// </summary>
internal static class ApiPlanOrphanMetadataRecovery
{
    internal const string RecoverySource = "KbInventory";

    private static readonly string[] ServiceSuffixes = { "List", "Get", "Create", "Update", "Delete" };

    private static readonly string[] SharedSdtNames =
    {
        "sdt_API_ErrorMessage",
        "sdt_API_ErrorResponse",
        "sdt_API_Pagination",
    };

    private static readonly string[] NotRecoveredSections =
    {
        "fields",
        "pagination",
        "order",
        "services",
        "levels",
        "transactionStructure",
    };

    /// <summary>
    /// Verifica se há uma metadata órfã recuperável e monta o inventário. Não grava nada.
    /// </summary>
    public static bool TryPrepare(
        KBModel designModel,
        Transaction transaction,
        out OrphanMetadataRecoveryPlan plan,
        out string detail)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        plan = null!;
        var apiName = "api" + transaction.Name;
        var metadataFileName = apiName + "_Metadata";

        // B082: duas varreduras respondem à elegibilidade. O índice completo custa sete, e no
        // caso comum — não há órfã — seria montado e descartado.
        var apiMatches = ApiPlanScanProbe.Scan(
            "API",
            "orphan-recovery-preflight",
            () => API.GetAll(designModel)
                .Where(item => string.Equals(item.Name, apiName, StringComparison.OrdinalIgnoreCase))
                .ToArray());
        if (apiMatches.Length != 1)
        {
            detail = $"API Object '{apiName}' encontrado {apiMatches.Length} vez(es); esperado exatamente 1.";
            return false;
        }

        var metadataMatches = ApiPlanScanProbe.Scan(
            "File",
            "orphan-recovery-preflight",
            () => WikiFileKBObject.GetAll(designModel)
                .Where(item => string.Equals(item.Name, metadataFileName, StringComparison.OrdinalIgnoreCase))
                .ToArray());
        var apiObject = apiMatches[0];

        WikiFileKBObject? staleFile = null;
        if (metadataMatches.Length > 1)
        {
            detail = $"File '{metadataFileName}' encontrado {metadataMatches.Length} vez(es); recuperação não se aplica com metadata ambígua.";
            return false;
        }

        if (metadataMatches.Length == 1)
        {
            // O File existe. Só há um caso recuperável: ele foi produzido por uma recuperação
            // anterior e o API Object que ele registra deixou de existir — tipicamente porque
            // foi removido e o Wizard criou outro com o mesmo nome. Aí a metadata aponta para
            // um GUID morto, o Wizard trava em OwnershipSchemaApiNameOrGuidMismatch, o Remover
            // recusa, e a recuperação não se oferecia porque o File estava lá. Beco sem saída.
            //
            // Numa metadata **completa** o mesmo descompasso não é recuperável aqui: o
            // fingerprint B067 cobre o conteúdo inteiro, e corrigir só o apiGuid trocaria um
            // bloqueio por outro. Esse caso continua exigindo decisão humana.
            if (!TryReadStaleRecoveredMetadata(metadataMatches[0], apiObject, out var mismatchDetail))
            {
                detail = mismatchDetail;
                return false;
            }

            staleFile = metadataMatches[0];
        }

        // Posse por dois sinais independentes, os mesmos que sustentam o ramo
        // OwnedByDescriptionFallback de ApiPlanApiObjectOwnership: a Description sentinela que
        // a extensão grava, e o Service Source chamando as Procedures geradas. Sem a metadata
        // não há como usar DiagnoseOwnership, que depende de um ApiPlan.
        if (!ApiPlanOwnedObjectDescription.IsCanonical(apiObject.Description, apiName))
        {
            detail = $"API Object '{apiName}' não tem a Description própria da extensão; recuperação recusada.";
            return false;
        }

        if (!CallsGeneratedProcedures(apiObject, transaction.Name))
        {
            detail = $"API Object '{apiName}' não chama Procedures 'proc{transaction.Name}_API_*'; recuperação recusada.";
            return false;
        }

        var procedures = FindOwnedProcedures(designModel, transaction.Name);
        if (procedures.Count == 0)
        {
            detail = $"Nenhuma Procedure própria 'proc{transaction.Name}_API_*' foi encontrada; não há inventário para registrar.";
            return false;
        }

        plan = new OrphanMetadataRecoveryPlan(
            apiName,
            metadataFileName,
            apiObject,
            FindTransactionFolderName(designModel, transaction),
            procedures,
            FindOwnedSdts(designModel, "sdt" + transaction.Name + "_API_"),
            FindSharedSdts(designModel),
            staleFile);

        var situation = staleFile is null
            ? $"metadata '{metadataFileName}' ausente"
            : $"metadata '{metadataFileName}' registra um API Object que não existe mais";
        detail = $"API Object próprio confirmado por Description e Service Source; {situation}. "
            + $"Inventário: Procedures={plan.ProcedureNames.Count}, SdtsProprios={plan.OwnSdtNames.Count}, SdtsCompartilhados={plan.SharedSdtNames.Count}.";
        return true;
    }

    /// <summary>
    /// Decide se uma metadata existente pode ser regravada pela recuperação. Só o caso da
    /// intenção importada com API Object trocado é recuperável — ver o comentário no chamador.
    /// </summary>
    private static bool TryReadStaleRecoveredMetadata(WikiFileKBObject file, API apiObject, out string detail)
    {
        JObject? metadata;
        try
        {
            var bytes = file.BlobPart?.Data?.GetBytes();
            metadata = bytes is null || bytes.Length == 0
                ? null
                : JObject.Parse(Encoding.UTF8.GetString(bytes));
        }
        catch (Exception exception)
        {
            detail = $"File '{file.Name}' existe mas não pôde ser lido ({exception.GetType().Name}); recuperação não se aplica.";
            return false;
        }

        if (metadata is null)
        {
            detail = $"File '{file.Name}' existe e está vazio; recuperação não se aplica.";
            return false;
        }

        if (!IsImportedRecovery(metadata))
        {
            detail = $"File '{file.Name}' existe e não foi produzido pela recuperação; recuperação não se aplica.";
            return false;
        }

        var recordedApiGuid = metadata.SelectToken("ownership.apiGuid")?.Value<string>();
        if (string.Equals(recordedApiGuid, apiObject.Guid.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            detail = $"File '{file.Name}' existe e já registra o API Object atual; recuperação não se aplica.";
            return false;
        }

        detail = $"File '{file.Name}' registra o API Object '{recordedApiGuid}', e o que existe é '{apiObject.Guid}'.";
        return true;
    }

    /// <summary>
    /// Grava a metadata de recuperação. Só cria o File; não altera API Object, Procedures ou SDTs.
    /// </summary>
    public static OrphanMetadataRecoveryResult Recover(
        KBModel designModel,
        Transaction transaction,
        OrphanMetadataRecoveryPlan plan)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        var json = CreateRecoveryJson(transaction, plan);
        var bytes = Encoding.UTF8.GetBytes(json);

        // Reusa o File quando a recuperação está corrigindo uma metadata importada cujo API
        // Object foi trocado; criar um segundo com o mesmo nome deixaria a KB ambígua e
        // bloquearia Remover e Sincronizar por metadata duplicada.
        var file = plan.StaleFile ?? new WikiFileKBObject(designModel);
        if (plan.StaleFile is null)
        {
            file.Name = plan.MetadataFileName;
        }

        file.Description = ApiPlanOwnedObjectDescription.Create(plan.MetadataFileName);
        if (transaction.Module is not null)
        {
            file.Module = transaction.Module;
        }

        file.SetPropertyValue("JavaExtract", false);
        file.SetPropertyValue("NetExtract", false);
        file.BlobPart.SetPropertyValue("FileName", plan.MetadataFileName + ".json");
        file.BlobPart.Data = BinaryStream.FromBytes(bytes);
        file.Save();

        var persisted = WikiFileKBObject.GetAll(designModel)
            .Single(item => string.Equals(item.Name, plan.MetadataFileName, StringComparison.OrdinalIgnoreCase));
        var persistedBytes = persisted.BlobPart?.Data?.GetBytes();
        if (persistedBytes is null || !persistedBytes.SequenceEqual(bytes))
        {
            throw new InvalidOperationException(
                $"Recuperação de metadata falhou: o File '{plan.MetadataFileName}' não preservou os bytes UTF-8 esperados.");
        }

        return new OrphanMetadataRecoveryResult(
            persisted.Name,
            persisted.Guid,
            bytes.Length,
            plan.ProcedureNames.Count,
            plan.OwnSdtNames.Count,
            plan.SharedSdtNames.Count);
    }

    /// <summary>
    /// Diz se uma metadata já lida foi produzida pela recuperação e ainda não foi reescrita
    /// por um Apply completo. Usado para bloquear o Sincronizar, que não tem contrato com que
    /// comparar.
    /// </summary>
    public static bool IsImportedRecovery(JObject? metadata) =>
        metadata?.SelectToken("recovery.imported")?.Value<bool>() == true;

    internal static string CreateRecoveryJson(Transaction transaction, OrphanMetadataRecoveryPlan plan)
    {
        // Só os blocos que a remoção consome, mais a marca. As demais seções ficam ausentes:
        // ver o comentário da classe e a seção 13.3 do plano da F3.
        var metadata = new JObject
        {
            ["schemaVersion"] = ApiPlanMetadataFileWriter.SchemaVersion,
            ["generator"] = "Genexus Open API Builder",
            ["generatedAtUtc"] = DateTime.UtcNow.ToString("O"),
            ["ownership"] = new JObject
            {
                ["descriptionSentinel"] = ApiPlanOwnedObjectDescription.Create(plan.MetadataFileName),
                ["transactionName"] = transaction.Name,
                ["transactionGuid"] = transaction.Guid.ToString(),
                ["apiName"] = plan.ApiName,
                ["apiGuid"] = plan.ApiObject.Guid.ToString(),
                ["metadataFileName"] = plan.MetadataFileName,
            },
            ["objects"] = new JObject
            {
                // wasCreated=false é conservador e deliberado: não há como saber se o Folder
                // foi criado pela extensão, e a remoção não deve apagar Folder de terceiro.
                ["transactionFolder"] = new JObject
                {
                    ["name"] = plan.TransactionFolderName,
                    ["wasCreated"] = false,
                },
                ["apiObject"] = new JObject
                {
                    ["name"] = plan.ApiName,
                    ["guid"] = plan.ApiObject.Guid.ToString(),
                },
                ["procedures"] = new JArray(plan.ProcedureNames),
                ["sdts"] = new JObject
                {
                    ["own"] = new JArray(plan.OwnSdtNames),
                    ["shared"] = new JArray(plan.SharedSdtNames),
                },
            },
            ["recovery"] = new JObject
            {
                ["imported"] = true,
                ["importedAtUtc"] = DateTime.UtcNow.ToString("O"),
                ["source"] = RecoverySource,
                ["notRecovered"] = new JArray(NotRecoveredSections),
            },
        };

        return metadata.ToString(Formatting.Indented);
    }

    private static bool CallsGeneratedProcedures(API apiObject, string transactionName)
    {
        var source = apiObject.ServiceGroupSource?.Source;
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        var pattern = new Regex(
            @"\bproc" + Regex.Escape(transactionName) + @"_API_(?:List|Get|Create|Update|Delete)\s*\(",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        return pattern.IsMatch(source!);
    }

    private static IReadOnlyList<string> FindOwnedProcedures(KBModel designModel, string transactionName)
    {
        var expected = ServiceSuffixes
            .Select(suffix => "proc" + transactionName + "_API_" + suffix)
            .ToArray();

        return ApiPlanScanProbe.Scan(
            "Procedure",
            "orphan-recovery-inventory",
            () => Procedure.GetAll(designModel)
                .Where(item => expected.Contains(item.Name, StringComparer.OrdinalIgnoreCase))
                .Where(item => ApiPlanOwnedObjectDescription.IsOwnedProcedure(item.Description, item.Name))
                .Select(item => item.Name)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static IReadOnlyList<string> FindOwnedSdts(KBModel designModel, string ownPrefix)
    {
        return ApiPlanScanProbe.Scan(
            "SDT",
            "orphan-recovery-inventory",
            () => SDT.GetAll(designModel)
                .Where(item => item.Name.StartsWith(ownPrefix, StringComparison.OrdinalIgnoreCase))
                .Where(item => ApiPlanOwnedObjectDescription.IsOwnedSdt(item.Description, item.Name))
                .Select(item => item.Name)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static IReadOnlyList<string> FindSharedSdts(KBModel designModel)
    {
        return ApiPlanScanProbe.Scan(
            "SDT",
            "orphan-recovery-inventory",
            () => SDT.GetAll(designModel)
                .Where(item => SharedSdtNames.Contains(item.Name, StringComparer.OrdinalIgnoreCase))
                .Where(item => ApiPlanOwnedObjectDescription.IsOwnedSdt(item.Description, item.Name))
                .Select(item => item.Name)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static string FindTransactionFolderName(KBModel designModel, Transaction transaction)
    {
        var expected = transaction.Name + "OpenApi";
        var found = ApiPlanScanProbe.Scan(
            "Folder",
            "orphan-recovery-inventory",
            () => Folder.GetAll(designModel)
                .Any(item => string.Equals(item.Name, expected, StringComparison.OrdinalIgnoreCase)));
        return found ? expected : string.Empty;
    }
}

internal sealed class OrphanMetadataRecoveryPlan
{
    public OrphanMetadataRecoveryPlan(
        string apiName,
        string metadataFileName,
        API apiObject,
        string transactionFolderName,
        IReadOnlyList<string> procedureNames,
        IReadOnlyList<string> ownSdtNames,
        IReadOnlyList<string> sharedSdtNames,
        WikiFileKBObject? staleFile = null)
    {
        ApiName = apiName;
        MetadataFileName = metadataFileName;
        ApiObject = apiObject;
        TransactionFolderName = transactionFolderName;
        ProcedureNames = procedureNames;
        OwnSdtNames = ownSdtNames;
        SharedSdtNames = sharedSdtNames;
        StaleFile = staleFile;
    }

    /// <summary>
    /// Metadata importada a ser regravada, quando a recuperação corrige um API Object trocado.
    /// Nula no caso comum, em que o File não existe.
    /// </summary>
    public WikiFileKBObject? StaleFile { get; }

    public string ApiName { get; }

    public string MetadataFileName { get; }

    public API ApiObject { get; }

    public string TransactionFolderName { get; }

    public IReadOnlyList<string> ProcedureNames { get; }

    public IReadOnlyList<string> OwnSdtNames { get; }

    public IReadOnlyList<string> SharedSdtNames { get; }
}

internal sealed class OrphanMetadataRecoveryResult
{
    public OrphanMetadataRecoveryResult(
        string fileName,
        Guid guid,
        int bytes,
        int procedureCount,
        int ownSdtCount,
        int sharedSdtCount)
    {
        FileName = fileName;
        Guid = guid;
        Bytes = bytes;
        ProcedureCount = procedureCount;
        OwnSdtCount = ownSdtCount;
        SharedSdtCount = sharedSdtCount;
    }

    public string FileName { get; }

    public Guid Guid { get; }

    public int Bytes { get; }

    public int ProcedureCount { get; }

    public int OwnSdtCount { get; }

    public int SharedSdtCount { get; }
}
