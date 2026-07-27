using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Artech.Architecture.Common.Objects;
using Artech.Common;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Wiki;
using GenexusOpenApiBuilder.Extension.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

internal static class ApiPlanMetadataFileWriter
{
    internal const string SchemaVersion = "GOAB_API_METADATA_B060_V1";
    private const string OwnedDescriptionPrefix = "Genexus Open API Builder B060 Metadata File";

    public static ApiPlanMetadataFileWriteResult CreateOrReencounter(KBModel designModel, Transaction transaction, ApiPlan apiPlan)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (!string.Equals(transaction.Name, apiPlan.TransactionName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Gravacao de metadata B060 bloqueada: o ApiPlan em memoria nao pertence a Transaction selecionada atual. Nenhuma alteracao foi feita.");
        }

        var apiObject = PreflightApiObject(designModel, apiPlan);
        var preflight = PreflightMetadataFile(designModel, transaction, apiPlan, apiObject);
        var json = CreateMetadataJson(transaction, apiPlan, apiObject);
        var bytes = Encoding.UTF8.GetBytes(json);
        var file = preflight.ExistingFile ?? new WikiFileKBObject(designModel);
        if (preflight.ExistingFile is null)
        {
            file.Name = apiPlan.MetadataFileName;
        }

        var externalFileName = CreateExternalFileName(apiPlan);
        file.Description = CreateOwnedDescription(apiPlan);
        SetExtractionFlags(file);
        file.BlobPart.SetPropertyValue("FileName", externalFileName);
        file.BlobPart.Data = BinaryStream.FromBytes(bytes);
        file.Save();

        var persisted = WikiFileKBObject.GetAll(designModel)
            .Single(item => string.Equals(item.Name, apiPlan.MetadataFileName, StringComparison.OrdinalIgnoreCase));
        var persistedBytes = persisted.BlobPart?.Data?.GetBytes();
        if (persistedBytes is null || !persistedBytes.SequenceEqual(bytes))
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 falhou: o File '{apiPlan.MetadataFileName}' nao preservou os bytes UTF-8 esperados.");
        }

        var persistedExternalFileName = persisted.BlobPart?.GetPropertyValue<string>("FileName");
        if (!string.Equals(persistedExternalFileName, externalFileName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 falhou: o File '{apiPlan.MetadataFileName}' nao preservou BlobPart FileName='{externalFileName}'.");
        }

        return new ApiPlanMetadataFileWriteResult(
            persisted.Name,
            persisted.Guid,
            preflight.ExistingFile is null ? ApiPlanMetadataFileWriteStatus.Created : ApiPlanMetadataFileWriteStatus.Reencountered,
            SchemaVersion,
            bytes.Length,
            ComputeSha256(bytes));
    }

    internal static string CreateOwnedDescription(ApiPlan apiPlan)
    {
        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        return $"{OwnedDescriptionPrefix} - Transaction={apiPlan.TransactionName} - Api={apiPlan.ApiName}";
    }

    private static API PreflightApiObject(KBModel designModel, ApiPlan apiPlan)
    {
        var matches = API.GetAll(designModel)
            .Where(api => string.Equals(api.Name, apiPlan.ApiName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length == 0)
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 bloqueada: API Object requerido nao foi reencontrado: '{apiPlan.ApiName}'. Execute B054/B055 antes. Nenhuma alteracao foi feita.");
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 bloqueada: foram encontrados {matches.Length} API Objects chamados '{apiPlan.ApiName}'. Nenhuma alteracao foi feita.");
        }

        var apiObject = matches[0];
        var expectedDescription = ApiPlanApiObjectWriter.CreateOwnedDescription(apiPlan);
        if (!string.Equals(apiObject.Description, expectedDescription, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 bloqueada: API Object externo ou incompativel chamado '{apiPlan.ApiName}'. Nenhuma alteracao foi feita.");
        }

        if (!ApiPlanBusinessComponentWriter.IsManagedApiObject(designModel, apiPlan, apiObject))
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 bloqueada: o API Object '{apiPlan.ApiName}' possui fonte ou variaveis divergentes da geracao B054/B055. Nenhuma alteracao foi feita.");
        }

        return apiObject;
    }

    private static ApiPlanMetadataFilePreflightResult PreflightMetadataFile(KBModel designModel, Transaction transaction, ApiPlan apiPlan, API apiObject)
    {
        var matches = WikiFileKBObject.GetAll(designModel)
            .Where(file => string.Equals(file.Name, apiPlan.MetadataFileName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length > 1)
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 bloqueada: foram encontrados {matches.Length} Files chamados '{apiPlan.MetadataFileName}'. Nenhuma alteracao foi feita.");
        }

        if (matches.Length == 0)
        {
            return new ApiPlanMetadataFilePreflightResult(null);
        }

        var file = matches[0];
        if (!string.Equals(file.Name, apiPlan.MetadataFileName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 bloqueada: o File '{apiPlan.MetadataFileName}' foi reencontrado com caixa divergente. Nenhuma alteracao foi feita.");
        }

        if (!string.Equals(file.Description, CreateOwnedDescription(apiPlan), StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 bloqueada: ja existe File externo ou incompativel chamado '{apiPlan.MetadataFileName}'. Nenhuma alteracao foi feita.");
        }

        ValidateExistingMetadata(file, transaction, apiPlan, apiObject);
        return new ApiPlanMetadataFilePreflightResult(file);
    }

    private static void ValidateExistingMetadata(WikiFileKBObject file, Transaction transaction, ApiPlan apiPlan, API apiObject)
    {
        var bytes = file.BlobPart?.Data?.GetBytes();
        if (bytes is null || bytes.Length == 0)
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 bloqueada: File proprio '{apiPlan.MetadataFileName}' nao possui JSON persistido. Nenhuma alteracao foi feita.");
        }

        JObject metadata;
        try
        {
            metadata = JObject.Parse(Encoding.UTF8.GetString(bytes));
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 bloqueada: File proprio '{apiPlan.MetadataFileName}' possui JSON invalido. Nenhuma alteracao foi feita.", ex);
        }

        RequireString(metadata, "schemaVersion", SchemaVersion, apiPlan.MetadataFileName);
        RequireString(metadata.SelectToken("ownership.transactionName"), apiPlan.TransactionName, "ownership.transactionName", apiPlan.MetadataFileName);
        RequireString(metadata.SelectToken("ownership.transactionGuid"), transaction.Guid.ToString(), "ownership.transactionGuid", apiPlan.MetadataFileName);
        RequireString(metadata.SelectToken("ownership.apiName"), apiPlan.ApiName, "ownership.apiName", apiPlan.MetadataFileName);
        RequireString(metadata.SelectToken("ownership.apiGuid"), apiObject.Guid.ToString(), "ownership.apiGuid", apiPlan.MetadataFileName);
        RequireString(metadata.SelectToken("ownership.metadataFileName"), apiPlan.MetadataFileName, "ownership.metadataFileName", apiPlan.MetadataFileName);
    }

    private static string CreateMetadataJson(Transaction transaction, ApiPlan apiPlan, API apiObject)
    {
        var metadata = new JObject
        {
            ["schemaVersion"] = SchemaVersion,
            ["generator"] = "Genexus Open API Builder",
            ["generatedAtUtc"] = DateTime.UtcNow.ToString("O"),
            ["ownership"] = new JObject
            {
                ["descriptionSentinel"] = CreateOwnedDescription(apiPlan),
                ["transactionName"] = apiPlan.TransactionName,
                ["transactionGuid"] = transaction.Guid.ToString(),
                ["transactionModule"] = apiPlan.ModuleTarget,
                ["apiName"] = apiPlan.ApiName,
                ["apiGuid"] = apiObject.Guid.ToString(),
                ["metadataFileName"] = apiPlan.MetadataFileName,
                ["restArtifactTarget"] = apiPlan.RestArtifactTarget,
                ["conflictMode"] = apiPlan.ConflictMode,
                ["reexecutionMode"] = apiPlan.ReexecutionMode,
            },
            ["api"] = new JObject
            {
                ["name"] = apiPlan.ApiName,
                ["servicesBasePath"] = apiPlan.ServicesBasePath,
                ["restPath"] = apiPlan.RestPath,
                ["endpointsCount"] = apiPlan.EndpointsCount,
            },
            ["objects"] = new JObject
            {
                ["transactionFolder"] = new JObject { ["name"] = apiPlan.TransactionFolderName, ["wasCreated"] = apiPlan.TransactionFolderWasCreated },
                ["apiObject"] = new JObject { ["name"] = apiPlan.ApiName, ["guid"] = apiObject.Guid.ToString() },
                ["procedures"] = ToStringArray(apiPlan.ProcedureNames),
                ["sdts"] = new JObject
                {
                    ["createRequest"] = apiPlan.CreateRequestSdtName,
                    ["updateRequest"] = apiPlan.UpdateRequestSdtName,
                    ["response"] = apiPlan.ResponseSdtName,
                    ["listFilters"] = apiPlan.ListFiltersSdtName,
                    ["listResponse"] = apiPlan.ListResponseSdtName,
                    ["shared"] = ToStringArray(apiPlan.SharedSdtNames),
                },
            },
            ["services"] = new JArray(apiPlan.Services.Select(service => new JObject
            {
                ["name"] = service.Name,
                ["httpMethod"] = service.HttpMethod,
                ["restPath"] = service.RestPath,
                ["operationId"] = service.OperationId,
                ["description"] = apiPlan.ServiceDescriptions.Single(description => string.Equals(description.ServiceName, service.Name, StringComparison.OrdinalIgnoreCase)).Description,
            })),
            ["fields"] = new JObject
            {
                ["primaryKey"] = ToFieldArray(apiPlan.PrimaryKey),
                ["createRequest"] = ToFieldArray(apiPlan.CreateRequestFields),
                ["updateRequest"] = ToFieldArray(apiPlan.UpdateRequestFields),
                ["response"] = ToFieldArray(apiPlan.ResponseFields),
                ["listFilters"] = new JArray(apiPlan.ListFilters.Select(filter => new JObject
                {
                    ["field"] = ToFieldObject(filter.Field),
                    ["operator"] = filter.FilterOperator,
                    ["usesPeriod"] = filter.UsesPeriod,
                    ["usesRange"] = filter.UsesRange,
                })),
                ["required"] = new JArray(apiPlan.RequiredFields.Select(field => new JObject
                {
                    ["requestName"] = field.RequestName,
                    ["fieldName"] = field.FieldName,
                    ["isRequired"] = field.IsRequired,
                    ["reason"] = field.Reason,
                })),
            },
            ["pagination"] = new JObject
            {
                ["defaultPageSize"] = apiPlan.DefaultPageSize,
                ["maximumPageSize"] = apiPlan.MaximumPageSize,
            },
            ["order"] = new JArray(apiPlan.StaticOrder.Select(order => new JObject
            {
                ["order"] = order.Order,
                ["attributeName"] = order.AttributeName,
                ["direction"] = order.Direction,
            })),
            ["security"] = new JObject
            {
                ["level"] = apiPlan.Security.SecurityLevel,
                ["gamCondition"] = apiPlan.Security.GamCondition,
                ["requiresGenerationConfirmation"] = apiPlan.Security.RequiresGenerationConfirmation,
                ["notes"] = ToStringArray(apiPlan.Security.Notes),
            },
            ["descriptions"] = new JObject
            {
                ["language"] = apiPlan.ServiceDescriptionLanguage,
                ["languageSource"] = apiPlan.ServiceDescriptionLanguageSource,
                ["fallbackUsed"] = apiPlan.ServiceDescriptionFallbackUsed,
                ["fallbackReason"] = apiPlan.ServiceDescriptionFallbackReason,
                ["services"] = new JArray(apiPlan.ServiceDescriptions.Select(description => new JObject
                {
                    ["serviceName"] = description.ServiceName,
                    ["description"] = description.Description,
                })),
            },
            ["classification"] = CreateClassificationObject(apiPlan.FieldClassificationConfiguration),
            ["businessComponent"] = new JObject
            {
                ["transactionName"] = apiPlan.BusinessComponent.TransactionName,
                ["isBusinessComponent"] = apiPlan.BusinessComponent.IsBusinessComponent,
                ["enabledDuringWizard"] = apiPlan.BusinessComponent.EnabledDuringWizard,
                ["status"] = apiPlan.BusinessComponent.Status,
            },
            ["engine"] = new JObject
            {
                ["generatorTarget"] = apiPlan.GeneratorTarget,
                ["isEngineReady"] = apiPlan.IsEngineReady,
                ["readinessNotes"] = ToStringArray(apiPlan.EngineReadinessNotes),
            },
            ["scope"] = new JObject
            {
                ["b060"] = "Initial persistent metadata File only",
                ["doesNotCompleteRest"] = true,
                ["doesNotApplyFinalHttpCodes"] = true,
                ["doesNotApplyFinalSecurity"] = true,
            },
        };

        var snapshotJson = metadata.ToString(Formatting.None);
        metadata["fingerprint"] = new JObject
        {
            ["algorithm"] = "SHA-256",
            ["scope"] = "metadataWithoutFingerprint",
            ["value"] = ComputeSha256(Encoding.UTF8.GetBytes(snapshotJson)),
        };

        return metadata.ToString(Formatting.Indented) + "\n";
    }

    private static JObject CreateClassificationObject(ApiPlanFieldClassificationConfiguration configuration)
    {
        return new JObject
        {
            ["scope"] = configuration.Scope,
            ["source"] = configuration.Source,
            ["status"] = configuration.Status,
            ["isPersistedMetadata"] = configuration.IsPersistedMetadata,
            ["isKnowledgeBaseConfigured"] = configuration.IsKnowledgeBaseConfigured,
            ["sensitiveExactNames"] = ToStringArray(configuration.SensitiveExactNames),
            ["auditSuffixes"] = ToStringArray(configuration.AuditSuffixes),
            ["metadataContract"] = new JObject
            {
                ["schemaVersion"] = configuration.MetadataContract.SchemaVersion,
                ["sectionName"] = configuration.MetadataContract.SectionName,
                ["sensitiveExactNamesMember"] = configuration.MetadataContract.SensitiveExactNamesMember,
                ["auditExactNamesMember"] = configuration.MetadataContract.AuditExactNamesMember,
                ["auditSuffixesMember"] = configuration.MetadataContract.AuditSuffixesMember,
                ["requiredMembers"] = ToStringArray(configuration.MetadataContract.RequiredMembers),
            },
            ["notes"] = ToStringArray(configuration.Notes),
        };
    }

    private static JArray ToFieldArray(IEnumerable<ApiPlanField> fields)
    {
        return new JArray(fields.Select(ToFieldObject));
    }

    private static JObject ToFieldObject(ApiPlanField field)
    {
        return new JObject
        {
            ["order"] = field.Order,
            ["name"] = field.Name,
            ["dataType"] = field.DataType,
            ["length"] = field.Length,
            ["decimals"] = field.Decimals,
            ["isPrimaryKey"] = field.IsPrimaryKey,
            ["isNullable"] = field.IsNullable,
            ["isSensitive"] = field.IsSensitive,
            ["isAuditField"] = field.IsAuditField,
            ["sensitiveClassificationSource"] = field.SensitiveClassificationSource,
            ["sensitiveClassificationReason"] = field.SensitiveClassificationReason,
            ["auditClassificationSource"] = field.AuditClassificationSource,
            ["auditClassificationReason"] = field.AuditClassificationReason,
            ["isFormula"] = field.IsFormula,
            ["isInferred"] = field.IsInferred,
            ["isRedundant"] = field.IsRedundant,
            ["isWritableByCreate"] = field.IsWritableByCreate,
            ["isWritableByUpdate"] = field.IsWritableByUpdate,
            ["isFilterEligible"] = field.IsFilterEligible,
        };
    }

    private static JArray ToStringArray(IEnumerable<string> values)
    {
        return new JArray(values.Select(value => new JValue(value)));
    }

    private static void RequireString(JToken? token, string expectedValue, string tokenPath, string fileName)
    {
        if (token is null || token.Type != JTokenType.String || !string.Equals(token.Value<string>(), expectedValue, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 bloqueada: File proprio '{fileName}' possui '{tokenPath}' incompativel. Nenhuma alteracao foi feita.");
        }
    }

    private static void RequireString(JObject metadata, string propertyName, string expectedValue, string fileName)
    {
        RequireString(metadata[propertyName], expectedValue, propertyName, fileName);
    }

    private static string CreateExternalFileName(ApiPlan apiPlan)
    {
        return apiPlan.MetadataFileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? apiPlan.MetadataFileName : apiPlan.MetadataFileName + ".json";
    }

    private static void SetExtractionFlags(WikiFileKBObject file)
    {
        file.SetPropertyValue("JavaExtract", false);
        file.SetPropertyValue("NetExtract", false);
        file.SetPropertyValue("NetCoreExtract", false);
        file.SetPropertyValue("IOSExtract", false);
        file.SetPropertyValue("AndroidExtract", false);
        file.SetPropertyValue("ExtractZip", false);
        TrySetOptionalProperty(file, "Extract", false);
    }

    private static void TrySetOptionalProperty(WikiFileKBObject file, string propertyId, object value)
    {
        try
        {
            file.SetPropertyValue(propertyId, value);
        }
        catch
        {
        }
    }

    private static string ComputeSha256(byte[] bytes)
    {
        using (var algorithm = SHA256.Create())
        {
            return BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", string.Empty);
        }
    }
}

internal static class ApiPlanMetadataFileWriteStatus
{
    public const string Created = "Created";
    public const string Reencountered = "Reencountered";
}

internal sealed class ApiPlanMetadataFilePreflightResult
{
    public ApiPlanMetadataFilePreflightResult(WikiFileKBObject? existingFile)
    {
        ExistingFile = existingFile;
    }

    public WikiFileKBObject? ExistingFile { get; }
}

internal sealed class ApiPlanMetadataFileWriteResult
{
    public ApiPlanMetadataFileWriteResult(string fileName, Guid guid, string status, string schemaVersion, int bytes, string sha256)
    {
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        Guid = guid;
        Status = status ?? throw new ArgumentNullException(nameof(status));
        SchemaVersion = schemaVersion ?? throw new ArgumentNullException(nameof(schemaVersion));
        Bytes = bytes;
        Sha256 = sha256 ?? throw new ArgumentNullException(nameof(sha256));
    }

    public string FileName { get; }

    public Guid Guid { get; }

    public string Status { get; }

    public string SchemaVersion { get; }

    public int Bytes { get; }

    public string Sha256 { get; }
}
