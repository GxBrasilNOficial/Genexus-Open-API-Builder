#nullable enable

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

public sealed class PrototypeWizardPreferenceValues
{
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

    public string SecurityLevelByDefault { get; set; } = PrototypeWizardPreferencesCodec.SecurityLevelAuthentication;

    public int DefaultPageSizeByDefault { get; set; } = PrototypeWizardPreferencesCodec.DefaultPageSizeFallback;

    public int MaximumPageSizeByDefault { get; set; } = PrototypeWizardPreferencesCodec.MaximumPageSizeFallback;

    public bool IncludeBusinessComponentErrorMessagesByDefault { get; set; } = true;
}

public static class PrototypeWizardPreferencesCodec
{
    public const string SchemaVersion = "GOAB_WIZARD_PREFERENCES_V1";
    public const string FileName = "GxOpenApiBuilder_Settings";
    public const string SecurityLevelAuthentication = "Authentication";
    public const string SecurityLevelAuthorization = "Authorization";
    public const string SecurityLevelNone = "None";
    public const int DefaultPageSizeFallback = 50;
    public const int MaximumPageSizeFallback = 200;

    public static PrototypeWizardPreferenceValues CreateDefault()
    {
        return new PrototypeWizardPreferenceValues();
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

    public static PrototypeWizardPreferenceValues Parse(string json)
    {
        var root = JObject.Parse(json);
        RequireString(root, "schemaVersion", SchemaVersion);
        var defaults = root["wizardDefaults"] as JObject
            ?? throw new JsonException("Membro obrigatorio 'wizardDefaults' ausente.");

        var preferences = new PrototypeWizardPreferenceValues
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
            SecurityLevelByDefault = NormalizeSecurityLevel(ReadOptionalString(defaults, "securityLevel", SecurityLevelAuthentication)),
            DefaultPageSizeByDefault = ReadOptionalPositiveInt(defaults["pagination"] as JObject, "defaultPageSize", DefaultPageSizeFallback),
            MaximumPageSizeByDefault = ReadOptionalPositiveInt(defaults["pagination"] as JObject, "maximumPageSize", MaximumPageSizeFallback),
            IncludeBusinessComponentErrorMessagesByDefault = ReadOptionalBool(defaults, "includeBusinessComponentErrorMessages", true),
        };

        Validate(preferences);
        return preferences;
    }

    public static string Serialize(PrototypeWizardPreferenceValues preferences)
    {
        if (preferences is null)
        {
            throw new ArgumentNullException(nameof(preferences));
        }

        Validate(preferences);
        var root = new JObject
        {
            ["schemaVersion"] = SchemaVersion,
            ["scope"] = "KnowledgeBase",
            ["fileName"] = FileName,
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
                ["securityLevel"] = NormalizeSecurityLevel(preferences.SecurityLevelByDefault),
                ["includeBusinessComponentErrorMessages"] = preferences.IncludeBusinessComponentErrorMessagesByDefault,
                ["pagination"] = new JObject
                {
                    ["defaultPageSize"] = preferences.DefaultPageSizeByDefault,
                    ["maximumPageSize"] = preferences.MaximumPageSizeByDefault,
                },
            },
        };

        return root.ToString(Formatting.Indented);
    }

    private static void Validate(PrototypeWizardPreferenceValues preferences)
    {
        if (preferences.DefaultPageSizeByDefault < 1 || preferences.MaximumPageSizeByDefault < 1)
        {
            throw new JsonException("Preferencias de paginacao invalidas: os tamanhos de pagina devem ser maiores ou iguais a 1.");
        }

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
}
