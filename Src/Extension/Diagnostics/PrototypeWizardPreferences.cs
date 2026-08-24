using System;
using System.Linq;
using System.Text;
using Artech.Architecture.Common.Objects;
using Artech.Common;
using Artech.Genexus.Common.Wiki;
using Newtonsoft.Json;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

internal sealed class PrototypeWizardPreferences
{
    public const string SchemaVersion = PrototypeWizardPreferencesCodec.SchemaVersion;
    public const string FileName = PrototypeWizardPreferencesCodec.FileName;
    public const string ExternalFileName = FileName + ".json";
    public const string OwnedDescriptionCanonical = "GxOpenApiBuilder_Settings - by Genexus Open API Builder";
    public const string OwnedDescription = OwnedDescriptionCanonical;
    public const string SecurityLevelAuthentication = PrototypeWizardPreferencesCodec.SecurityLevelAuthentication;
    public const string SecurityLevelAuthorization = PrototypeWizardPreferencesCodec.SecurityLevelAuthorization;
    public const string SecurityLevelNone = PrototypeWizardPreferencesCodec.SecurityLevelNone;
    public const int DefaultPageSizeFallback = PrototypeWizardPreferencesCodec.DefaultPageSizeFallback;
    public const int MaximumPageSizeFallback = PrototypeWizardPreferencesCodec.MaximumPageSizeFallback;

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

    public bool IncludeBusinessComponentErrorMessagesByDefault { get; set; } = true;

    public static PrototypeWizardPreferences CreateDefault()
    {
        return FromPreferenceValues(PrototypeWizardPreferencesCodec.CreateDefault());
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
            IncludeBusinessComponentErrorMessagesByDefault = IncludeBusinessComponentErrorMessagesByDefault,
        };
    }

    public static string NormalizeSecurityLevel(string? value)
    {
        return PrototypeWizardPreferencesCodec.NormalizeSecurityLevel(value);
    }

    public static PrototypeWizardPreferences FromPreferenceValues(PrototypeWizardPreferenceValues values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        return new PrototypeWizardPreferences
        {
            GenerateSdtsByDefault = values.GenerateSdtsByDefault,
            GenerateProceduresByDefault = values.GenerateProceduresByDefault,
            GenerateApiObjectByDefault = values.GenerateApiObjectByDefault,
            GenerateMetadataByDefault = values.GenerateMetadataByDefault,
            ApplyListByDefault = values.ApplyListByDefault,
            ApplyBusinessComponentByDefault = values.ApplyBusinessComponentByDefault,
            ListServiceByDefault = values.ListServiceByDefault,
            GetServiceByDefault = values.GetServiceByDefault,
            CreateServiceByDefault = values.CreateServiceByDefault,
            UpdateServiceByDefault = values.UpdateServiceByDefault,
            SecurityLevelByDefault = values.SecurityLevelByDefault,
            DefaultPageSizeByDefault = values.DefaultPageSizeByDefault,
            MaximumPageSizeByDefault = values.MaximumPageSizeByDefault,
            IncludeBusinessComponentErrorMessagesByDefault = values.IncludeBusinessComponentErrorMessagesByDefault,
        };
    }

    public PrototypeWizardPreferenceValues ToPreferenceValues()
    {
        return new PrototypeWizardPreferenceValues
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
            IncludeBusinessComponentErrorMessagesByDefault = IncludeBusinessComponentErrorMessagesByDefault,
        };
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
            || !ApiPlanOwnedObjectDescription.IsOwnedPreferencesFile(file.Description))
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
                || !ApiPlanOwnedObjectDescription.IsOwnedPreferencesFile(existingFile.Description)))
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
        return PrototypeWizardPreferences.FromPreferenceValues(PrototypeWizardPreferencesCodec.Parse(json));
    }

    private static string Serialize(PrototypeWizardPreferences preferences)
    {
        return PrototypeWizardPreferencesCodec.Serialize(preferences.ToPreferenceValues());
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
