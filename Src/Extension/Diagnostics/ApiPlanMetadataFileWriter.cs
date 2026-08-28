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
    internal const string SchemaVersionV1 = "GOAB_API_METADATA_B060_V1";
    internal const string SchemaVersion = "GOAB_API_METADATA_B060_V2";
    internal static readonly string[] SupportedSchemaVersions =
    {
        SchemaVersionV1,
        SchemaVersion,
    };
    internal const string B067IntegrityVersion = ApiPlanMetadataIntegrity.Version;

    internal static bool IsSupportedSchemaVersion(string? schemaVersion)
    {
        if (string.IsNullOrWhiteSpace(schemaVersion))
        {
            return false;
        }

        for (var index = 0; index < SupportedSchemaVersions.Length; index++)
        {
            if (string.Equals(schemaVersion, SupportedSchemaVersions[index], StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static ApiPlanMetadataFileWriteResult CreateOrReencounter(KBModel designModel, Transaction transaction, ApiPlan apiPlan)
    {
        return CreateOrReencounter(designModel, transaction, apiPlan, allowIntentionalContractRefresh: false);
    }

    public static ApiPlanMetadataFileWriteResult CreateOrReencounter(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        bool allowIntentionalContractRefresh)
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

        var apiObject = PreflightApiObject(designModel, apiPlan, allowIntentionalContractRefresh);
        var preflight = PreflightMetadataFile(designModel, transaction, apiPlan, apiObject, allowIntentionalContractRefresh);
        var json = CreateMetadataJson(transaction, apiPlan, apiObject);
        var bytes = Encoding.UTF8.GetBytes(json);
        var file = preflight.ExistingFile ?? new WikiFileKBObject(designModel);
        if (preflight.ExistingFile is null)
        {
            file.Name = apiPlan.MetadataFileName;
        }

        var externalFileName = CreateExternalFileName(apiPlan);
        file.Description = CreateOwnedDescription(apiPlan);
        AlignWithTransactionModule(file, transaction);
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
            ComputeSha256(bytes),
            B067IntegrityVersion,
            ComputePlannedContractHash(apiPlan));
    }

    internal static string CreateOwnedDescription(ApiPlan apiPlan)
    {
        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        return ApiPlanOwnedObjectDescription.Create(apiPlan.MetadataFileName);
    }

    private static void AlignWithTransactionModule(WikiFileKBObject file, Transaction transaction)
    {
        if (transaction.Module is not null)
        {
            file.Module = transaction.Module;
        }
    }

    private static API PreflightApiObject(KBModel designModel, ApiPlan apiPlan, bool allowIntentionalContractRefresh)
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
        var owned = allowIntentionalContractRefresh
            ? ApiPlanApiObjectWriter.IsOwnedApiObjectForIntentionalWrite(designModel, apiPlan, apiObject)
            : ApiPlanApiObjectWriter.IsOwnedApiObject(designModel, apiPlan, apiObject);
        if (!owned)
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 bloqueada: API Object externo ou incompativel chamado '{apiPlan.ApiName}'. Nenhuma alteracao foi feita.");
        }

        return apiObject;
    }

    private static ApiPlanMetadataFilePreflightResult PreflightMetadataFile(
        KBModel designModel,
        Transaction transaction,
        ApiPlan apiPlan,
        API apiObject,
        bool allowIntentionalContractRefresh)
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

        if (!ApiPlanOwnedObjectDescription.IsOwnedMetadataFile(file.Description, apiPlan.MetadataFileName, apiPlan.TransactionName))
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 bloqueada: ja existe File externo ou incompativel chamado '{apiPlan.MetadataFileName}'. Nenhuma alteracao foi feita.");
        }

        ValidateExistingMetadata(file, transaction, apiPlan, apiObject, allowIntentionalContractRefresh);
        return new ApiPlanMetadataFilePreflightResult(file);
    }

    private static void ValidateExistingMetadata(
        WikiFileKBObject file,
        Transaction transaction,
        ApiPlan apiPlan,
        API apiObject,
        bool allowIntentionalContractRefresh)
    {
        var bytes = file.BlobPart?.Data?.GetBytes();
        if (bytes is null || bytes.Length == 0)
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 bloqueada: File proprio '{apiPlan.MetadataFileName}' nao possui JSON persistido. Nenhuma alteracao foi feita.");
        }

        JObject metadata;
        try
        {
            metadata = ApiPlanMetadataIntegrity.ParseMetadataBytes(bytes);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 bloqueada: File proprio '{apiPlan.MetadataFileName}' possui JSON invalido. Nenhuma alteracao foi feita.", ex);
        }

        RequireSupportedSchemaVersion(metadata["schemaVersion"], apiPlan.MetadataFileName);
        RequireString(metadata.SelectToken("ownership.transactionName"), apiPlan.TransactionName, "ownership.transactionName", apiPlan.MetadataFileName);
        RequireString(metadata.SelectToken("ownership.transactionGuid"), transaction.Guid.ToString(), "ownership.transactionGuid", apiPlan.MetadataFileName);
        RequireString(metadata.SelectToken("ownership.apiName"), apiPlan.ApiName, "ownership.apiName", apiPlan.MetadataFileName);
        RequireString(metadata.SelectToken("ownership.apiGuid"), apiObject.Guid.ToString(), "ownership.apiGuid", apiPlan.MetadataFileName);
        RequireString(metadata.SelectToken("ownership.metadataFileName"), apiPlan.MetadataFileName, "ownership.metadataFileName", apiPlan.MetadataFileName);
        if (!allowIntentionalContractRefresh)
        {
            ValidateB067IntegrityIfPresent(metadata, apiPlan, apiObject);
        }
    }

    internal static bool HasCompatibleB067Integrity(JObject metadata, ApiPlan apiPlan, API apiObject)
    {
        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        if (apiObject is null)
        {
            throw new ArgumentNullException(nameof(apiObject));
        }

        return ApiPlanMetadataIntegrity.HasCompatibleIntegrity(
            metadata,
            ComputeServiceDescriptionsHash(apiPlan),
            ComputeCompatiblePlannedContractHashes(metadata, apiPlan),
            ComputeActualServiceDescriptionsHash(apiPlan, apiObject.ServiceGroupSource.Source),
            ApiPlanApiObjectWriter.CreateOwnedDescriptionCandidates(apiPlan),
            ComputeCompatibleExpectedServiceSources(apiPlan),
            ApiPlanBusinessComponentWriter.IsManagedApiObject(apiObject.Model, apiPlan, apiObject));
    }

    /// <summary>
    /// Confere se a metadata e o API Object ainda correspondem ao ultimo
    /// estado gravado pela extensao. Nao usa o ApiPlan novo: uma mudanca feita
    /// pelo Wizard ou pelo Sincronizar deve poder produzir um novo contrato.
    /// </summary>
    internal static bool HasCompatibleGeneratedBaseline(JObject metadata, API apiObject)
    {
        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        if (apiObject is null)
        {
            throw new ArgumentNullException(nameof(apiObject));
        }

        if (!HasCompatibleMetadataFingerprint(metadata))
        {
            return false;
        }

        var integrity = metadata["integrity"] as JObject;
        if (integrity is null)
        {
            return true;
        }

        var serviceNames = ((JArray?)integrity.SelectToken("generatedDescriptions.services"))
            ?.Select(item => item["serviceName"]?.Value<string>() ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
        if (serviceNames is null || serviceNames.Length == 0)
        {
            return false;
        }

        var actualDescriptionsHash = ApiPlanMetadataIntegrity.ComputeJsonSha256(
            ApiPlanMetadataIntegrity.CreateServiceDescriptionsContractFromSource(
                apiObject.ServiceGroupSource.Source,
                serviceNames));
        var actualSourceHash = ApiPlanMetadataIntegrity.ComputeNormalizedTextSha256(apiObject.ServiceGroupSource.Source);
        return ApiPlanMetadataIntegrity.HasCompatibleGeneratedBaseline(
            metadata,
            actualDescriptionsHash,
            actualSourceHash,
            apiObject.Description,
            apiObject.Guid.ToString());
    }

    private static void ValidateB067IntegrityIfPresent(JObject metadata, ApiPlan apiPlan, API apiObject)
    {
        if (!HasCompatibleB067Integrity(metadata, apiPlan, apiObject))
        {
            throw new InvalidOperationException($"Gravacao de metadata B067 bloqueada: File proprio '{apiPlan.MetadataFileName}' indica alteracao manual posterior em descricoes, ownership ou contrato essencial. Nenhuma alteracao foi feita.");
        }
    }

    internal static bool HasCompatibleMetadataFingerprint(JObject metadata)
    {
        return ApiPlanMetadataIntegrity.DiagnoseMetadataFingerprint(metadata).IsCompatible;
    }

    private static string CreateMetadataJson(Transaction transaction, ApiPlan apiPlan, API apiObject)
    {
        var transactionStructure = BuildTransactionStructure(transaction);
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
                    ["own"] = ToStringArray(ApiPlanGeneratedApiRemovalInventory.BuildOwnSdtNamesForRemoval(apiPlan)),
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
            ["transactionStructure"] = ToFieldArray(transactionStructure),
            ["levels"] = ApiPlanMetadataLevelsCodec.CreateLevelsToken(apiPlan),
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
                    ["attributeGuid"] = ResolveRequiredAttributeGuid(apiPlan, field),
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
                ["attributeGuid"] = ResolveOrderAttributeGuid(apiPlan, order.AttributeName, transactionStructure),
                ["direction"] = order.Direction,
            })),
            ["security"] = new JObject
            {
                ["level"] = apiPlan.Security.SecurityLevel,
                ["gamCondition"] = apiPlan.Security.GamCondition,
                ["requiresGenerationConfirmation"] = apiPlan.Security.RequiresGenerationConfirmation,
                ["notes"] = ToStringArray(apiPlan.Security.Notes),
            },
            ["errorDetail"] = new JObject
            {
                ["includeBusinessComponentMessages"] = apiPlan.IncludeBusinessComponentErrorMessages,
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
            ["integrity"] = CreateB067IntegrityObject(apiPlan, apiObject, transactionStructure),
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

    private static JObject CreateB067IntegrityObject(ApiPlan apiPlan, API apiObject, IReadOnlyList<ApiPlanField>? transactionStructure = null)
    {
        return ApiPlanMetadataIntegrity.Create(
            CreateServiceDescriptionsContract(apiPlan),
            CreatePlannedContract(apiPlan, transactionStructure: transactionStructure, includePagination: false),
            ApiPlanApiObjectWriter.CreateOwnedDescription(apiPlan),
            apiObject.Guid.ToString(),
            ResolveServiceSourceMode(apiPlan, apiObject),
            apiObject.ServiceGroupSource.Source,
            ComputeExpectedServiceSource(apiPlan, apiObject));
    }

    private static JArray CreateServiceDescriptionsContract(ApiPlan apiPlan)
    {
        return new JArray(apiPlan.ServiceDescriptions
            .OrderBy(description => description.ServiceName, StringComparer.Ordinal)
            .Select(description => new JObject
            {
                ["serviceName"] = description.ServiceName,
                ["description"] = description.Description,
            }));
    }

    private static JObject CreatePlannedContract(
        ApiPlan apiPlan,
        bool useLegacyPathParameterSyntax = false,
        IReadOnlyList<ApiPlanField>? transactionStructure = null,
        bool includePagination = true)
    {
        var contract = new JObject
        {
            ["api"] = new JObject
            {
                ["name"] = apiPlan.ApiName,
                ["servicesBasePath"] = apiPlan.ServicesBasePath,
                ["restPath"] = NormalizePlannedContractRestPath(apiPlan.RestPath, useLegacyPathParameterSyntax),
                ["securityLevel"] = apiPlan.Security.SecurityLevel,
                ["gamCondition"] = apiPlan.Security.GamCondition,
            },
            ["services"] = new JArray(apiPlan.Services.Select(service => new JObject
            {
                ["name"] = service.Name,
                ["httpMethod"] = service.HttpMethod,
                ["restPath"] = NormalizePlannedContractRestPath(service.RestPath, useLegacyPathParameterSyntax),
                ["operationId"] = service.OperationId,
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
                    ["attributeGuid"] = ResolveRequiredAttributeGuid(apiPlan, field),
                    ["isRequired"] = field.IsRequired,
                    ["reason"] = field.Reason,
                })),
            },
            ["order"] = new JArray(apiPlan.StaticOrder.Select(order => new JObject
            {
                ["order"] = order.Order,
                ["attributeName"] = order.AttributeName,
                ["attributeGuid"] = ResolveOrderAttributeGuid(apiPlan, order.AttributeName, transactionStructure),
                ["direction"] = order.Direction,
            })),
        };

        if (includePagination)
        {
            contract["pagination"] = new JObject
            {
                ["defaultPageSize"] = apiPlan.DefaultPageSize,
                ["maximumPageSize"] = apiPlan.MaximumPageSize,
            };
        }

        var levels = ApiPlanMetadataLevelsCodec.CreateLevelsToken(apiPlan);
        if (levels is not null)
        {
            contract["levels"] = levels;
        }

        return contract;
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
            ["attributeGuid"] = field.AttributeGuid,
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

    private static IReadOnlyList<ApiPlanField> BuildTransactionStructure(Transaction transaction)
    {
        return PrototypeWizardContractReader.Read(transaction).Attributes
            .OrderBy(item => item.Order)
            .Select(item => new ApiPlanField(
                item.Order,
                item.AttributeGuid,
                item.Name,
                item.DataType,
                item.Length,
                item.Decimals,
                item.IsPrimaryKey,
                item.IsNullable,
                item.IsSensitive,
                item.IsAudit,
                item.SensitiveClassificationSource,
                item.SensitiveClassificationReason,
                item.AuditClassificationSource,
                item.AuditClassificationReason,
                item.IsFormula,
                item.IsInferred,
                item.IsRedundant,
                item.IsPayloadEligible,
                item.IsUpdatePayloadEligible,
                item.IsFilterEligible))
            .ToArray();
    }

    private static string ResolveRequiredAttributeGuid(ApiPlan apiPlan, ApiPlanRequiredField field)
    {
        var source = string.Equals(field.RequestName, "UpdateRequest", StringComparison.OrdinalIgnoreCase)
            ? apiPlan.UpdateRequestFields
            : apiPlan.CreateRequestFields;
        var match = source.FirstOrDefault(item => string.Equals(item.Name, field.FieldName, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            throw new InvalidOperationException($"Campo required '{field.RequestName}.{field.FieldName}' nao foi encontrado no ApiPlan para gravar attributeGuid.");
        }

        return match.AttributeGuid;
    }

    private static string ResolveOrderAttributeGuid(ApiPlan apiPlan, string attributeName, IReadOnlyList<ApiPlanField>? transactionStructure = null)
    {
        var match = (transactionStructure ?? Array.Empty<ApiPlanField>())
            .Concat(apiPlan.PrimaryKey)
            .Concat(apiPlan.ResponseFields)
            .Concat(apiPlan.CreateRequestFields)
            .Concat(apiPlan.UpdateRequestFields)
            .Concat(apiPlan.ListFilters.Select(filter => filter.Field))
            .FirstOrDefault(item => string.Equals(item.Name, attributeName, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            throw new InvalidOperationException($"Atributo de ordenacao '{attributeName}' nao foi encontrado no ApiPlan para gravar attributeGuid.");
        }

        return match.AttributeGuid;
    }

    private static JArray ToStringArray(IEnumerable<string> values)
    {
        return new JArray(values.Select(value => new JValue(value)));
    }

    private static bool HasString(JToken? token, string expectedValue)
    {
        return token is not null && token.Type == JTokenType.String && string.Equals(token.Value<string>(), expectedValue, StringComparison.Ordinal);
    }

    private static void RequireString(JToken? token, string expectedValue, string tokenPath, string fileName)
    {
        if (token is null || token.Type != JTokenType.String || !string.Equals(token.Value<string>(), expectedValue, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 bloqueada: File proprio '{fileName}' possui '{tokenPath}' incompativel. Nenhuma alteracao foi feita.");
        }
    }

    private static void RequireSupportedSchemaVersion(JToken? token, string fileName)
    {
        var actual = token is not null && token.Type == JTokenType.String ? token.Value<string>() : null;
        if (!IsSupportedSchemaVersion(actual))
        {
            throw new InvalidOperationException($"Gravacao de metadata B060 bloqueada: File proprio '{fileName}' possui 'schemaVersion' incompativel. Nenhuma alteracao foi feita.");
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

    internal static string ComputePlannedContractHash(ApiPlan apiPlan)
    {
        if (apiPlan is null)
        {
            throw new ArgumentNullException(nameof(apiPlan));
        }

        return ApiPlanMetadataIntegrity.ComputeJsonSha256(CreatePlannedContract(apiPlan, includePagination: false));
    }

    private static string ComputeServiceDescriptionsHash(ApiPlan apiPlan)
    {
        return ApiPlanMetadataIntegrity.ComputeJsonSha256(CreateServiceDescriptionsContract(apiPlan));
    }

    private static string ComputeActualServiceDescriptionsHash(ApiPlan apiPlan, string source)
    {
        return ApiPlanMetadataIntegrity.ComputeJsonSha256(ApiPlanMetadataIntegrity.CreateServiceDescriptionsContractFromSource(source, apiPlan.Services.Select(service => service.Name)));
    }

    private static string ComputeExpectedServiceSource(ApiPlan apiPlan, API apiObject)
    {
        if (ApiPlanListProcedureWriter.IsB070ApiObject(apiObject.Model, apiPlan, apiObject))
        {
            return ApiPlanListProcedureWriter.CreateB070ServiceGroupSource(
                apiPlan,
                includeBusinessComponentParameters: IsActualB070WithBusinessComponent(apiPlan, apiObject));
        }

        return ApiPlanBusinessComponentWriter.IsB055ApiObject(apiObject.Model, apiPlan, apiObject)
            ? ApiPlanBusinessComponentWriter.CreateB055ServiceGroupSource(apiPlan)
            : ApiPlanBusinessComponentWriter.CreateB054ServiceGroupSource(apiPlan);
    }

    private static IReadOnlyList<string> ComputeCompatibleExpectedServiceSources(ApiPlan apiPlan)
    {
        return new[]
        {
            ApiPlanBusinessComponentWriter.CreateB054ServiceGroupSource(apiPlan),
            ApiPlanBusinessComponentWriter.CreateB055ServiceGroupSource(apiPlan),
            ApiPlanBusinessComponentWriter.CreateB079InternalErrorOnlyServiceGroupSource(apiPlan),
            ApiPlanListProcedureWriter.CreateB070ServiceGroupSource(apiPlan, includeBusinessComponentParameters: false),
            ApiPlanListProcedureWriter.CreateB070ServiceGroupSource(apiPlan, includeBusinessComponentParameters: true),
            ApiPlanListProcedureWriter.CreateB070InternalErrorOnlyServiceGroupSource(apiPlan, includeBusinessComponentParameters: true),
        }
        .SelectMany(CreateCompatibleServiceSourceVariants)
        .Distinct(StringComparer.Ordinal)
        .ToArray();
    }

    private static IEnumerable<string> CreateCompatibleServiceSourceVariants(string source)
    {
        var legacyPath = NormalizeServiceSourceRestPath(source, useLegacyPathParameterSyntax: true);
        var withoutPut = RemoveServiceSourceLine(source, "[RestMethod(PUT)]");
        var withoutRestPath = RemoveServiceSourceRestPathAnnotations(source);
        var legacyPathWithoutPut = RemoveServiceSourceLine(legacyPath, "[RestMethod(PUT)]");
        var legacyPathWithoutRestPath = RemoveServiceSourceRestPathAnnotations(legacyPath);
        var withoutPutOrRestPath = RemoveServiceSourceRestPathAnnotations(withoutPut);
        var legacyPathWithoutPutOrRestPath = RemoveServiceSourceRestPathAnnotations(legacyPathWithoutPut);

        var withoutSecurityLevel = RemoveServiceSourceSecurityLevelAnnotations(source);

        return new[]
        {
            source,
            legacyPath,
            withoutPut,
            withoutRestPath,
            withoutSecurityLevel,
            legacyPathWithoutPut,
            legacyPathWithoutRestPath,
            withoutPutOrRestPath,
            legacyPathWithoutPutOrRestPath,
            RemoveServiceSourceSecurityLevelAnnotations(legacyPath),
            RemoveServiceSourceSecurityLevelAnnotations(withoutPut),
            RemoveServiceSourceSecurityLevelAnnotations(withoutRestPath),
        };
    }

    private static string[] ComputeCompatiblePlannedContractHashes(JObject metadata, ApiPlan apiPlan)
    {
        var hashes = new List<string>
        {
            ComputePlannedContractHash(apiPlan),
            ApiPlanMetadataIntegrity.ComputeJsonSha256(CreatePlannedContract(apiPlan, useLegacyPathParameterSyntax: true, includePagination: false)),
            // Metadata B067 generated before pagination became mutable included
            // the page limits in the essential-contract hash. Keep both legacy
            // variants accepted so existing APIs can be reencountered safely.
            ApiPlanMetadataIntegrity.ComputeJsonSha256(CreatePlannedContract(apiPlan)),
            ApiPlanMetadataIntegrity.ComputeJsonSha256(CreatePlannedContract(apiPlan, useLegacyPathParameterSyntax: true)),
        };

        var storedContract = metadata.SelectToken("integrity.plannedContract.contract") as JObject;
        if (storedContract is not null)
        {
            var storedContractWithoutPagination = (JObject)storedContract.DeepClone();
            storedContractWithoutPagination.Remove("pagination");
            var currentContractWithoutPagination = CreatePlannedContract(apiPlan, includePagination: false);
            var currentLegacyContractWithoutPagination = CreatePlannedContract(apiPlan, useLegacyPathParameterSyntax: true, includePagination: false);
            if (JToken.DeepEquals(storedContractWithoutPagination, currentContractWithoutPagination) ||
                JToken.DeepEquals(storedContractWithoutPagination, currentLegacyContractWithoutPagination))
            {
                // The stored legacy hash is accepted only when removing
                // pagination leaves the current essential contract unchanged.
                hashes.Add(ApiPlanMetadataIntegrity.ComputeJsonSha256(storedContract));
            }
        }

        return hashes
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string NormalizePlannedContractRestPath(string restPath, bool useLegacyPathParameterSyntax)
    {
        if (!useLegacyPathParameterSyntax || string.IsNullOrEmpty(restPath))
        {
            return restPath;
        }

        return restPath.Replace("{&", "{");
    }

    private static string NormalizeServiceSourceRestPath(string source, bool useLegacyPathParameterSyntax)
    {
        if (!useLegacyPathParameterSyntax || string.IsNullOrEmpty(source))
        {
            return source;
        }

        return source.Replace("{&", "{");
    }

    private static string RemoveServiceSourceLine(string source, string exactTrimmedLine)
    {
        return string.Join(
            Environment.NewLine,
            NormalizeForComparison(source)
                .Split('\n')
                .Where(line => !string.Equals(line.Trim(), exactTrimmedLine, StringComparison.OrdinalIgnoreCase)));
    }

    private static string RemoveServiceSourceRestPathAnnotations(string source)
    {
        return string.Join(
            Environment.NewLine,
            NormalizeForComparison(source)
                .Split('\n')
                .Where(line => !line.TrimStart().StartsWith("[RestPath(", StringComparison.OrdinalIgnoreCase)));
    }

    private static string RemoveServiceSourceSecurityLevelAnnotations(string source)
    {
        return string.Join(
            Environment.NewLine,
            NormalizeForComparison(source)
                .Split('\n')
                .Where(line => !line.TrimStart().StartsWith("[SecurityLevel(", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsActualB070WithBusinessComponent(ApiPlan apiPlan, API apiObject)
    {
        return string.Equals(
            NormalizeForComparison(apiObject.ServiceGroupSource.Source),
            NormalizeForComparison(ApiPlanListProcedureWriter.CreateB070ServiceGroupSource(apiPlan, includeBusinessComponentParameters: true)),
            StringComparison.Ordinal);
    }

    private static string ResolveServiceSourceMode(ApiPlan apiPlan, API apiObject)
    {
        if (ApiPlanListProcedureWriter.IsB070ApiObject(apiObject.Model, apiPlan, apiObject))
        {
            return "B070";
        }

        return ApiPlanBusinessComponentWriter.IsB055ApiObject(apiObject.Model, apiPlan, apiObject) ? "B055" : "B054";
    }

    private static string NormalizeForComparison(string? value) => (value ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n").Trim();

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
    public ApiPlanMetadataFileWriteResult(string fileName, Guid guid, string status, string schemaVersion, int bytes, string sha256, string integrityVersion, string plannedContractHash)
    {
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        Guid = guid;
        Status = status ?? throw new ArgumentNullException(nameof(status));
        SchemaVersion = schemaVersion ?? throw new ArgumentNullException(nameof(schemaVersion));
        Bytes = bytes;
        Sha256 = sha256 ?? throw new ArgumentNullException(nameof(sha256));
        IntegrityVersion = integrityVersion ?? throw new ArgumentNullException(nameof(integrityVersion));
        PlannedContractHash = plannedContractHash ?? throw new ArgumentNullException(nameof(plannedContractHash));
    }

    public string FileName { get; }

    public Guid Guid { get; }

    public string Status { get; }

    public string SchemaVersion { get; }

    public int Bytes { get; }

    public string Sha256 { get; }

    public string IntegrityVersion { get; }

    public string PlannedContractHash { get; }
}
