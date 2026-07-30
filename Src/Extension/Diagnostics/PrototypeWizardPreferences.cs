using System;
using System.Linq;
using System.Text;
using Artech.Architecture.Common.Objects;
using Artech.Common;
using Artech.Genexus.Common.Wiki;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

internal sealed class PrototypeWizardPreferences
{
    public const string SchemaVersion = "GOAB_WIZARD_PREFERENCES_V1";
    public const string FileName = "GxOpenApiBuilder_Settings";
    public const string ExternalFileName = FileName + ".json";
    public const string OwnedDescription = "Genexus Open API Builder Wizard Preferences";
    public const string SecurityLevelAuthentication = "Authentication";
    public const string SecurityLevelAuthorization = "Authorization";
    public const string SecurityLevelNone = "None";
    public const int DefaultPageSizeFallback = 50;
    public const int MaximumPageSizeFallback = 200;

    public bool GenerateSdtsByDefault { get; set; }

    public bool GenerateProceduresByDefault { get; set; }

    public bool GenerateApiObjectByDefault { get; set; }

    public bool GenerateMetadataByDefault { get; set; }

    public bool ApplyListByDefault { get; set; }

    public bool ApplyBusinessComponentByDefault { get; set; }

    public bool ListServiceByDefault { get; set; } = true;

    public bool GetServiceByDefault { get; set; } = true;

    public bool CreateServiceByDefault { get; set; } = true;

    public bool UpdateServiceByDefault { get; set; } = true;

    public string SecurityLevelByDefault { get; set; } = SecurityLevelAuthentication;

    public int DefaultPageSizeByDefault { get; set; } = DefaultPageSizeFallback;

    public int MaximumPageSizeByDefault { get; set; } = MaximumPageSizeFallback;

    public static PrototypeWizardPreferences CreateDefault()
    {
        return new PrototypeWizardPreferences();
    }

    public PrototypeWizardPreferences Clone()
    {
        return new PrototypeWizardPreferences
        {
            GenerateSdtsByDefault = GenerateSdtsByDefault,
            GenerateProceduresByDefault = GenerateProceduresByDefault,
            GenerateApiObjectByDefault = GenerateApiObjectByDefault,
            GenerateMetadataByDefault = GenerateMetadataByDefault,
            ApplyListByDefault = ApplyListByDefault,
            ApplyBusinessComponentByDefault = ApplyBusinessComponentByDefault,
            ListServiceByDefault = ListServiceByDefault,
            GetServiceByDefault = GetServiceByDefault,
            CreateServiceByDefault = CreateServiceByDefault,
            UpdateServiceByDefault = UpdateServiceByDefault,
            SecurityLevelByDefault = SecurityLevelByDefault,
            DefaultPageSizeByDefault = DefaultPageSizeByDefault,
            MaximumPageSizeByDefault = MaximumPageSizeByDefault,
        };
    }

    public static string NormalizeSecurityLevel(string? value)
    {
        if (string.Equals(value, SecurityLevelAuthorization, StringComparison.OrdinalIgnoreCase))
        {
            return SecurityLevelAuthorization;
        }

        if (string.Equals(value, SecurityLevelNone, StringComparison.OrdinalIgnoreCase))
        {
            return SecurityLevelNone;
        }

        return SecurityLevelAuthentication;
    }
}

internal sealed class PrototypeWizardPreferencesLoadResult
{
    public PrototypeWizardPreferencesLoadResult(PrototypeWizardPreferences preferences, bool loadedFromKnowledgeBase, string status)
    {
        Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        LoadedFromKnowledgeBase = loadedFromKnowledgeBase;
        Status = status ?? throw new ArgumentNullException(nameof(status));
    }

    public PrototypeWizardPreferences Preferences { get; }

    public bool LoadedFromKnowledgeBase { get; }

    public string Status { get; }
}

internal sealed class PrototypeWizardPreferencesSaveResult
{
    public PrototypeWizardPreferencesSaveResult(string fileName, Guid guid, bool created, int bytes)
    {
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        Guid = guid;
        Created = created;
        Bytes = bytes;
    }

    public string FileName { get; }

    public Guid Guid { get; }

    public bool Created { get; }

    public int Bytes { get; }
}

internal static class PrototypeWizardPreferencesStore
{
    public static PrototypeWizardPreferencesLoadResult Load(KBModel designModel)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        var matches = FindFiles(designModel);
        if (matches.Length == 0)
        {
            return new PrototypeWizardPreferencesLoadResult(
                PrototypeWizardPreferences.CreateDefault(),
                false,
                "Preferencias do wizard ausentes na KB ativa; defaults conservadores em memoria aplicados.");
        }

        if (matches.Length > 1)
        {
            return LoadDefaultWithWarning($"Foram encontrados {matches.Length} Files chamados '{PrototypeWizardPreferences.FileName}'. Defaults conservadores em memoria aplicados.");
        }

        var file = matches[0];
        if (!string.Equals(file.Name, PrototypeWizardPreferences.FileName, StringComparison.Ordinal)
            || !string.Equals(file.Description, PrototypeWizardPreferences.OwnedDescription, StringComparison.Ordinal))
        {
            return LoadDefaultWithWarning($"File '{PrototypeWizardPreferences.FileName}' externo ou incompativel encontrado. Defaults conservadores em memoria aplicados.");
        }

        var bytes = file.BlobPart?.Data?.GetBytes();
        if (bytes is null || bytes.Length == 0)
        {
            return LoadDefaultWithWarning($"File proprio '{PrototypeWizardPreferences.FileName}' nao possui JSON de preferencias. Defaults conservadores em memoria aplicados.");
        }

        try
        {
            var preferences = Parse(Encoding.UTF8.GetString(bytes));
            return new PrototypeWizardPreferencesLoadResult(
                preferences,
                true,
                $"Preferencias do wizard carregadas da KB ativa: File='{PrototypeWizardPreferences.FileName}'.");
        }
        catch (JsonException ex)
        {
            return LoadDefaultWithWarning($"File proprio '{PrototypeWizardPreferences.FileName}' possui JSON invalido: {ex.Message}. Defaults conservadores em memoria aplicados.");
        }
    }

    public static PrototypeWizardPreferencesSaveResult Save(KBModel designModel, PrototypeWizardPreferences preferences)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (preferences is null)
        {
            throw new ArgumentNullException(nameof(preferences));
        }

        var matches = FindFiles(designModel);
        if (matches.Length > 1)
        {
            throw new InvalidOperationException($"Gravacao de preferencias bloqueada: foram encontrados {matches.Length} Files chamados '{PrototypeWizardPreferences.FileName}'. Nenhuma alteracao foi feita.");
        }

        var existingFile = matches.SingleOrDefault();
        if (existingFile is not null
            && (!string.Equals(existingFile.Name, PrototypeWizardPreferences.FileName, StringComparison.Ordinal)
                || !string.Equals(existingFile.Description, PrototypeWizardPreferences.OwnedDescription, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Gravacao de preferencias bloqueada: ja existe File externo ou incompativel chamado '{PrototypeWizardPreferences.FileName}'. Nenhuma alteracao foi feita.");
        }

        var json = Serialize(preferences);
        var bytes = Encoding.UTF8.GetBytes(json);
        var file = existingFile ?? new WikiFileKBObject(designModel);
        if (existingFile is null)
        {
            file.Name = PrototypeWizardPreferences.FileName;
        }

        file.Description = PrototypeWizardPreferences.OwnedDescription;
        SetExtractionFlags(file);
        file.BlobPart.SetPropertyValue("FileName", PrototypeWizardPreferences.ExternalFileName);
        file.BlobPart.Data = BinaryStream.FromBytes(bytes);
        file.Save();

        var persisted = WikiFileKBObject.GetAll(designModel)
            .Single(item => string.Equals(item.Name, PrototypeWizardPreferences.FileName, StringComparison.OrdinalIgnoreCase));
        var persistedBytes = persisted.BlobPart?.Data?.GetBytes();
        if (persistedBytes is null || !persistedBytes.SequenceEqual(bytes))
        {
            throw new InvalidOperationException($"Gravacao de preferencias falhou: o File '{PrototypeWizardPreferences.FileName}' nao preservou os bytes UTF-8 esperados.");
        }

        var persistedExternalFileName = persisted.BlobPart?.GetPropertyValue<string>("FileName");
        if (!string.Equals(persistedExternalFileName, PrototypeWizardPreferences.ExternalFileName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Gravacao de preferencias falhou: o File '{PrototypeWizardPreferences.FileName}' nao preservou BlobPart FileName='{PrototypeWizardPreferences.ExternalFileName}'.");
        }

        return new PrototypeWizardPreferencesSaveResult(
            persisted.Name,
            persisted.Guid,
            existingFile is null,
            bytes.Length);
    }

    private static PrototypeWizardPreferencesLoadResult LoadDefaultWithWarning(string status)
    {
        return new PrototypeWizardPreferencesLoadResult(
            PrototypeWizardPreferences.CreateDefault(),
            false,
            status);
    }

    private static WikiFileKBObject[] FindFiles(KBModel designModel)
    {
        return WikiFileKBObject.GetAll(designModel)
            .Where(file => string.Equals(file.Name, PrototypeWizardPreferences.FileName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static PrototypeWizardPreferences Parse(string json)
    {
        var root = JObject.Parse(json);
        RequireString(root, "schemaVersion", PrototypeWizardPreferences.SchemaVersion);
        var defaults = root["wizardDefaults"] as JObject
            ?? throw new JsonException("Membro obrigatorio 'wizardDefaults' ausente.");

        var preferences = new PrototypeWizardPreferences
        {
            GenerateSdtsByDefault = ReadBool(defaults, "generateSdts"),
            GenerateProceduresByDefault = ReadBool(defaults, "generateProcedures"),
            GenerateApiObjectByDefault = ReadBool(defaults, "generateApiObject"),
            GenerateMetadataByDefault = ReadBool(defaults, "generateMetadata"),
            ApplyListByDefault = ReadBool(defaults, "applyList"),
            ApplyBusinessComponentByDefault = ReadBool(defaults, "applyBusinessComponent"),
            ListServiceByDefault = ReadOptionalBool(defaults["services"] as JObject, "list", true),
            GetServiceByDefault = ReadOptionalBool(defaults["services"] as JObject, "get", true),
            CreateServiceByDefault = ReadOptionalBool(defaults["services"] as JObject, "create", true),
            UpdateServiceByDefault = ReadOptionalBool(defaults["services"] as JObject, "update", true),
            SecurityLevelByDefault = PrototypeWizardPreferences.NormalizeSecurityLevel(ReadOptionalString(defaults, "securityLevel", PrototypeWizardPreferences.SecurityLevelAuthentication)),
            DefaultPageSizeByDefault = ReadOptionalPositiveInt(defaults["pagination"] as JObject, "defaultPageSize", PrototypeWizardPreferences.DefaultPageSizeFallback),
            MaximumPageSizeByDefault = ReadOptionalPositiveInt(defaults["pagination"] as JObject, "maximumPageSize", PrototypeWizardPreferences.MaximumPageSizeFallback),
        };

        if (preferences.DefaultPageSizeByDefault > preferences.MaximumPageSizeByDefault)
        {
            throw new JsonException("Preferencias de paginacao invalidas: 'defaultPageSize' deve ser menor ou igual a 'maximumPageSize'.");
        }

        if (!preferences.ListServiceByDefault
            && !preferences.GetServiceByDefault
            && !preferences.CreateServiceByDefault
            && !preferences.UpdateServiceByDefault)
        {
            throw new JsonException("Preferencias de servico invalidas: ao menos um servico deve iniciar marcado.");
        }

        return preferences;
    }

    private static string Serialize(PrototypeWizardPreferences preferences)
    {
        var root = new JObject
        {
            ["schemaVersion"] = PrototypeWizardPreferences.SchemaVersion,
            ["scope"] = "KnowledgeBase",
            ["fileName"] = PrototypeWizardPreferences.FileName,
            ["wizardDefaults"] = new JObject
            {
                ["generateSdts"] = preferences.GenerateSdtsByDefault,
                ["generateProcedures"] = preferences.GenerateProceduresByDefault,
                ["generateApiObject"] = preferences.GenerateApiObjectByDefault,
                ["generateMetadata"] = preferences.GenerateMetadataByDefault,
                ["applyList"] = preferences.ApplyListByDefault,
                ["applyBusinessComponent"] = preferences.ApplyBusinessComponentByDefault,
                ["services"] = new JObject
                {
                    ["list"] = preferences.ListServiceByDefault,
                    ["get"] = preferences.GetServiceByDefault,
                    ["create"] = preferences.CreateServiceByDefault,
                    ["update"] = preferences.UpdateServiceByDefault,
                },
                ["securityLevel"] = PrototypeWizardPreferences.NormalizeSecurityLevel(preferences.SecurityLevelByDefault),
                ["pagination"] = new JObject
                {
                    ["defaultPageSize"] = Math.Max(1, preferences.DefaultPageSizeByDefault),
                    ["maximumPageSize"] = Math.Max(1, preferences.MaximumPageSizeByDefault),
                },
            },
        };

        return root.ToString(Formatting.Indented);
    }

    private static void RequireString(JObject root, string propertyName, string expectedValue)
    {
        var actual = root[propertyName]?.Value<string>();
        if (!string.Equals(actual, expectedValue, StringComparison.Ordinal))
        {
            throw new JsonException($"Membro '{propertyName}' esperado='{expectedValue}', atual='{actual ?? "<ausente>"}'.");
        }
    }

    private static bool ReadBool(JObject root, string propertyName)
    {
        var token = root[propertyName];
        if (token is null || token.Type != JTokenType.Boolean)
        {
            throw new JsonException($"Membro booleano obrigatorio '{propertyName}' ausente ou invalido.");
        }

        return token.Value<bool>();
    }

    private static bool ReadOptionalBool(JObject? root, string propertyName, bool fallback)
    {
        if (root is null)
        {
            return fallback;
        }

        var token = root[propertyName];
        if (token is null)
        {
            return fallback;
        }

        if (token.Type != JTokenType.Boolean)
        {
            throw new JsonException($"Membro booleano opcional '{propertyName}' invalido.");
        }

        return token.Value<bool>();
    }

    private static string ReadOptionalString(JObject root, string propertyName, string fallback)
    {
        var token = root[propertyName];
        if (token is null)
        {
            return fallback;
        }

        if (token.Type != JTokenType.String)
        {
            throw new JsonException($"Membro string opcional '{propertyName}' invalido.");
        }

        return token.Value<string>() ?? fallback;
    }

    private static int ReadOptionalPositiveInt(JObject? root, string propertyName, int fallback)
    {
        if (root is null)
        {
            return fallback;
        }

        var token = root[propertyName];
        if (token is null)
        {
            return fallback;
        }

        if (token.Type != JTokenType.Integer)
        {
            throw new JsonException($"Membro inteiro opcional '{propertyName}' invalido.");
        }

        var value = token.Value<int>();
        if (value < 1)
        {
            throw new JsonException($"Membro inteiro opcional '{propertyName}' deve ser maior ou igual a 1.");
        }

        return value;
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
}
