#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// S-B111 / F3 — serialização canônica e leitura do diário durável.
///
/// O JSON persistido é o próprio material canônico: propriedades na ordem do schema, arrays
/// na ordem persistida, campos anuláveis presentes como <c>null</c>, arrays vazios como
/// <c>[]</c>, GUIDs no formato <c>D</c> minúsculo, timestamps UTC com precisão fixa de
/// milissegundos e sufixo <c>Z</c>, sem espaços supérfluos. Duas implementações que sigam
/// estas regras produzem byte a byte o mesmo material — que é o que dá sentido ao
/// <c>snapshotHash</c> usado na autorização de continuação.
///
/// O hash do snapshot é o SHA-256, em hexadecimal minúsculo, desse material. Ele não inclui
/// <c>journalFileId</c> nem qualquer dado de armazenamento externo, não inclui a autorização
/// de recuperação e nunca inclui a si mesmo. O digest dos bytes crus do File é verificação
/// separada de durabilidade e não substitui este valor.
///
/// A gravação é sempre validada antes: o serializer recusa um envelope que viole o contrato
/// do schema V1, para que nenhuma violação chegue ao <c>File.Save()</c>.
/// </summary>
public static class ApiPlanOperationJournalSerializer
{
    private const string UtcFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

    /// <summary>
    /// Serializa o envelope no material canônico, depois de validá-lo. Lança quando o
    /// envelope viola o schema V1.
    /// </summary>
    public static string Serialize(ApiPlanOperationJournal journal)
    {
        if (journal is null)
        {
            throw new ArgumentNullException(nameof(journal));
        }

        var validation = ApiPlanOperationJournalValidator.Validate(journal);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                "Gravação do diário bloqueada: o envelope viola o schema V1. " + validation.Describe());
        }

        return WriteCanonical(journal);
    }

    /// <summary>
    /// Bytes UTF-8 do material canônico, sem BOM. É o conteúdo exato do File do diário.
    /// </summary>
    public static byte[] SerializeToBytes(ApiPlanOperationJournal journal) =>
        new UTF8Encoding(false).GetBytes(Serialize(journal));

    /// <summary>
    /// SHA-256 hexadecimal minúsculo do material canônico do envelope.
    /// </summary>
    public static string ComputeSnapshotHash(ApiPlanOperationJournal journal)
    {
        if (journal is null)
        {
            throw new ArgumentNullException(nameof(journal));
        }

        return ComputeSnapshotHash(WriteCanonical(journal));
    }

    /// <summary>
    /// SHA-256 hexadecimal minúsculo de um material canônico já serializado.
    /// </summary>
    public static string ComputeSnapshotHash(string canonicalJson)
    {
        if (canonicalJson is null)
        {
            throw new ArgumentNullException(nameof(canonicalJson));
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(new UTF8Encoding(false).GetBytes(canonicalJson));
        var builder = new StringBuilder(hash.Length * 2);
        foreach (var value in hash)
        {
            builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    public static ApiPlanOperationJournalReadResult ReadBytes(byte[] bytes)
    {
        if (bytes is null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        return Read(new UTF8Encoding(false).GetString(bytes));
    }

    /// <summary>
    /// Lê e valida o JSON do diário. Nunca lança por conteúdo: devolve o resultado com os
    /// erros, porque a indisponibilidade do diário é diagnóstico, não exceção de fluxo.
    /// </summary>
    public static ApiPlanOperationJournalReadResult Read(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        JObject root;
        try
        {
            using var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None };
            var token = JToken.ReadFrom(reader);
            if (token is not JObject parsed)
            {
                return ApiPlanOperationJournalReadResult.Failed("O diário deve ser um objeto JSON.");
            }

            root = parsed;
        }
        catch (JsonException exception)
        {
            return ApiPlanOperationJournalReadResult.Failed("JSON inválido: " + Clean(exception.Message));
        }

        var errors = new List<string>();
        var schemaVersion = ReadInt(root, "schemaVersion", errors);
        if (schemaVersion.HasValue && schemaVersion.Value != ApiPlanOperationJournal.SchemaVersion)
        {
            // Versão desconhecida bloqueia a operação e a recuperação; não há migração
            // destrutiva automática.
            errors.Add(string.Format(
                CultureInfo.InvariantCulture,
                "schemaVersion desconhecida: esperado {0}, encontrado {1}.",
                ApiPlanOperationJournal.SchemaVersion,
                schemaVersion.Value));
        }

        var journalKind = ReadString(root, "journalKind", errors);
        if (journalKind is not null && !string.Equals(journalKind, ApiPlanOperationJournal.JournalKind, StringComparison.Ordinal))
        {
            errors.Add("journalKind deve ser " + ApiPlanOperationJournal.JournalKind + ".");
        }

        var journal = new ApiPlanOperationJournal
        {
            KnowledgeBaseGuid = ReadGuid(root, "knowledgeBaseGuid", errors),
            TransactionGuid = ReadGuid(root, "transactionGuid", errors),
            TransactionName = ReadString(root, "transactionName", errors) ?? string.Empty,
            OperationId = ReadGuid(root, "operationId", errors),
            ApplicationId = ReadGuid(root, "applicationId", errors),
            OperationKind = ReadEnum<JournalOperationKind>(root, "operationKind", errors),
            GeneratorVersion = ReadString(root, "generatorVersion", errors) ?? string.Empty,
            CreatedUtc = ReadUtc(root, "createdUtc", errors),
            UpdatedUtc = ReadUtc(root, "updatedUtc", errors),
            EnvelopePhase = ReadEnum<JournalEnvelopePhase>(root, "envelopePhase", errors),
            OperationState = ReadEnum<JournalOperationState>(root, "operationState", errors),
            LogicalStage = ReadEnum<JournalLogicalStage>(root, "logicalStage", errors),
            JournalDurability = ReadEnum<JournalDurability>(root, "journalDurability", errors),
            IntentKind = ReadEnum<JournalIntentKind>(root, "intentKind", errors),
            MetadataSchemaVersion = ReadOptionalString(root, "metadataSchemaVersion", errors),
            BlockReason = ReadOptionalEnum<JournalBlockReason>(root, "blockReason", errors),
        };

        ReadPlan(root, journal, errors);
        ReadInventory(root, journal, errors);
        ReadReceipts(root, journal, errors);
        ReadAbandonment(root, journal, errors);

        if (errors.Count > 0)
        {
            return ApiPlanOperationJournalReadResult.Failed(errors);
        }

        var validation = ApiPlanOperationJournalValidator.Validate(journal);
        if (!validation.IsValid)
        {
            return ApiPlanOperationJournalReadResult.Failed(validation.Errors);
        }

        return ApiPlanOperationJournalReadResult.Succeeded(journal, WriteCanonical(journal));
    }

    private static string WriteCanonical(ApiPlanOperationJournal journal)
    {
        var builder = new StringBuilder();
        using (var writer = new JsonTextWriter(new StringWriter(builder, CultureInfo.InvariantCulture))
        {
            Formatting = Formatting.None,
            Culture = CultureInfo.InvariantCulture,
            StringEscapeHandling = StringEscapeHandling.Default,
        })
        {
            writer.WriteStartObject();
            WriteProperty(writer, "schemaVersion", ApiPlanOperationJournal.SchemaVersion);
            WriteProperty(writer, "journalKind", ApiPlanOperationJournal.JournalKind);
            WriteProperty(writer, "knowledgeBaseGuid", journal.KnowledgeBaseGuid);
            WriteProperty(writer, "transactionGuid", journal.TransactionGuid);
            WriteProperty(writer, "transactionName", journal.TransactionName);
            WriteProperty(writer, "operationId", journal.OperationId);
            WriteProperty(writer, "applicationId", journal.ApplicationId);
            WriteProperty(writer, "operationKind", journal.OperationKind.ToString());
            WriteProperty(writer, "generatorVersion", journal.GeneratorVersion);
            WriteProperty(writer, "createdUtc", FormatUtc(journal.CreatedUtc));
            WriteProperty(writer, "updatedUtc", FormatUtc(journal.UpdatedUtc));
            WriteProperty(writer, "envelopePhase", journal.EnvelopePhase.ToString());
            WriteProperty(writer, "operationState", journal.OperationState.ToString());
            WriteProperty(writer, "logicalStage", journal.LogicalStage.ToString());
            WriteProperty(writer, "journalDurability", journal.JournalDurability.ToString());
            WriteProperty(writer, "intentKind", journal.IntentKind.ToString());
            WriteProperty(writer, "metadataSchemaVersion", journal.MetadataSchemaVersion);

            writer.WritePropertyName("plan");
            WritePlan(writer, journal.Plan);

            writer.WritePropertyName("inventory");
            writer.WriteStartArray();
            foreach (var item in journal.Inventory)
            {
                WriteInventoryItem(writer, item);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("receipts");
            writer.WriteStartArray();
            foreach (var receipt in journal.Receipts)
            {
                WriteReceipt(writer, receipt);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("abandonment");
            if (journal.Abandonment is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteStartObject();
                WriteProperty(writer, "reason", journal.Abandonment.Reason);
                WriteProperty(writer, "authorizedUtc", FormatUtc(journal.Abandonment.AuthorizedUtc));
                WriteProperty(writer, "authorizedBy", journal.Abandonment.AuthorizedBy);
                writer.WriteEndObject();
            }

            WriteProperty(writer, "blockReason", journal.BlockReason?.ToString());
            writer.WriteEndObject();
        }

        return builder.ToString();
    }

    private static void WritePlan(JsonTextWriter writer, ApiPlanOperationJournalPlan plan)
    {
        writer.WriteStartObject();
        WriteProperty(writer, "planKind", plan.PlanKind.ToString());
        WriteProperty(writer, "plannedApiGuid", plan.PlannedApiGuid);
        WriteProperty(writer, "contractHash", plan.ContractHash);
        WriteProperty(writer, "generateApiObject", plan.GenerateApiObject);
        WriteProperty(writer, "generateSdts", plan.GenerateSdts);
        WriteProperty(writer, "generateProcedures", plan.GenerateProcedures);
        WriteProperty(writer, "generateMetadata", plan.GenerateMetadata);
        writer.WritePropertyName("services");
        writer.WriteStartArray();
        foreach (var service in plan.Services)
        {
            writer.WriteValue(service ?? string.Empty);
        }

        writer.WriteEndArray();
        WriteProperty(writer, "inventorySufficiency", plan.InventorySufficiency?.ToString());
        writer.WriteEndObject();
    }

    private static void WriteInventoryItem(JsonTextWriter writer, ApiPlanOperationJournalInventoryItem item)
    {
        writer.WriteStartObject();
        WriteProperty(writer, "objectType", item.ObjectType.ToString());
        WriteProperty(writer, "identityKind", item.IdentityKind.ToString());
        WriteProperty(writer, "guid", item.Guid);
        WriteProperty(writer, "fileId", item.FileId);
        writer.WritePropertyName("composite");
        if (item.Composite is null)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteStartObject();
            WriteProperty(writer, "exactName", item.Composite.ExactName);
            WriteProperty(writer, "objectTypeName", item.Composite.ObjectTypeName);
            WriteProperty(writer, "role", item.Composite.Role);
            WriteProperty(writer, "canonicalDescription", item.Composite.CanonicalDescription);
            WriteProperty(writer, "transactionGuid", item.Composite.TransactionGuid);
            WriteProperty(writer, "apiGuid", item.Composite.ApiGuid);
            writer.WriteEndObject();
        }

        WriteProperty(writer, "emptyConfirmed", item.EmptyConfirmed);
        WriteProperty(writer, "name", item.Name);
        WriteProperty(writer, "ownershipValidated", item.OwnershipValidated);
        WriteProperty(writer, "action", item.Action.ToString());
        WriteProperty(writer, "physicalState", item.PhysicalState.ToString());
        WriteProperty(writer, "confirmation", item.Confirmation.ToString());
        WriteProperty(writer, "expectedHash", item.ExpectedHash);
        writer.WritePropertyName("receiptSequences");
        writer.WriteStartArray();
        foreach (var sequence in item.ReceiptSequences)
        {
            writer.WriteValue(sequence);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteReceipt(JsonTextWriter writer, ApiPlanOperationJournalReceipt receipt)
    {
        writer.WriteStartObject();
        WriteProperty(writer, "sequence", receipt.Sequence);
        WriteProperty(writer, "operation", receipt.Operation.ToString());
        WriteProperty(writer, "stage", receipt.Stage);
        WriteProperty(writer, "objectType", receipt.ObjectType.ToString());
        WriteProperty(writer, "attempt", receipt.Attempt);
        WriteProperty(writer, "retryOfSequence", receipt.RetryOfSequence);
        WriteProperty(writer, "startedUtc", FormatUtc(receipt.StartedUtc));
        WriteProperty(writer, "endedUtc", receipt.EndedUtc);
        WriteProperty(writer, "durationMs", receipt.DurationMs);
        WriteProperty(writer, "attemptState", receipt.AttemptState.ToString());
        WriteProperty(writer, "result", receipt.Result.ToString());
        WriteProperty(writer, "confirmation", receipt.Confirmation.ToString());
        WriteProperty(writer, "physicalState", receipt.PhysicalState.ToString());
        WriteProperty(writer, "retryEligible", receipt.RetryEligible);
        WriteProperty(writer, "retryableReason", receipt.RetryableReason?.ToString());
        writer.WriteEndObject();
    }

    private static void WriteProperty(JsonTextWriter writer, string name, string? value)
    {
        writer.WritePropertyName(name);
        if (value is null)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteValue(value);
        }
    }

    private static void WriteProperty(JsonTextWriter writer, string name, Guid value)
    {
        writer.WritePropertyName(name);
        writer.WriteValue(value.ToString("D", CultureInfo.InvariantCulture));
    }

    private static void WriteProperty(JsonTextWriter writer, string name, Guid? value)
    {
        writer.WritePropertyName(name);
        if (value.HasValue)
        {
            writer.WriteValue(value.Value.ToString("D", CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNull();
        }
    }

    private static void WriteProperty(JsonTextWriter writer, string name, int value)
    {
        writer.WritePropertyName(name);
        writer.WriteValue(value);
    }

    private static void WriteProperty(JsonTextWriter writer, string name, int? value)
    {
        writer.WritePropertyName(name);
        if (value.HasValue)
        {
            writer.WriteValue(value.Value);
        }
        else
        {
            writer.WriteNull();
        }
    }

    private static void WriteProperty(JsonTextWriter writer, string name, bool value)
    {
        writer.WritePropertyName(name);
        writer.WriteValue(value);
    }

    private static void WriteProperty(JsonTextWriter writer, string name, bool? value)
    {
        writer.WritePropertyName(name);
        if (value.HasValue)
        {
            writer.WriteValue(value.Value);
        }
        else
        {
            writer.WriteNull();
        }
    }

    internal static string FormatUtc(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

        return utc.ToString(UtcFormat, CultureInfo.InvariantCulture);
    }

    private static void WriteProperty(JsonTextWriter writer, string name, long value)
    {
        writer.WritePropertyName(name);
        writer.WriteValue(value);
    }

    private static void WriteProperty(JsonTextWriter writer, string name, DateTime? value)
    {
        writer.WritePropertyName(name);
        if (value.HasValue)
        {
            writer.WriteValue(FormatUtc(value.Value));
        }
        else
        {
            writer.WriteNull();
        }
    }

    private static void ReadPlan(JObject root, ApiPlanOperationJournal journal, List<string> errors)
    {
        if (root["plan"] is not JObject plan)
        {
            errors.Add("plan é obrigatório e deve ser um objeto.");
            return;
        }

        journal.Plan = new ApiPlanOperationJournalPlan
        {
            PlanKind = ReadEnum<JournalPlanKind>(plan, "plan.planKind", errors),
            PlannedApiGuid = ReadOptionalGuid(plan, "plan.plannedApiGuid", errors),
            ContractHash = ReadOptionalString(plan, "plan.contractHash", errors),
            GenerateApiObject = ReadOptionalBool(plan, "plan.generateApiObject", errors),
            GenerateSdts = ReadOptionalBool(plan, "plan.generateSdts", errors),
            GenerateProcedures = ReadOptionalBool(plan, "plan.generateProcedures", errors),
            GenerateMetadata = ReadOptionalBool(plan, "plan.generateMetadata", errors),
            InventorySufficiency = ReadOptionalEnum<JournalInventorySufficiency>(
                plan, "plan.inventorySufficiency", errors),
        };

        if (plan["services"] is not JArray services)
        {
            errors.Add("plan.services é obrigatório e deve ser um array.");
            return;
        }

        foreach (var service in services)
        {
            if (service.Type != JTokenType.String || string.IsNullOrWhiteSpace(service.Value<string>()))
            {
                errors.Add("plan.services só aceita strings não vazias.");
                continue;
            }

            journal.Plan.Services.Add(service.Value<string>()!);
        }
    }

    private static void ReadInventory(JObject root, ApiPlanOperationJournal journal, List<string> errors)
    {
        if (root["inventory"] is not JArray inventory)
        {
            errors.Add("inventory é obrigatório e deve ser um array.");
            return;
        }

        foreach (var token in inventory)
        {
            if (token is not JObject entry)
            {
                errors.Add("inventory só aceita objetos.");
                continue;
            }

            var item = new ApiPlanOperationJournalInventoryItem
            {
                ObjectType = ReadEnum<JournalObjectType>(entry, "inventory[].objectType", errors),
                IdentityKind = ReadEnum<JournalIdentityKind>(entry, "inventory[].identityKind", errors),
                Guid = ReadOptionalGuid(entry, "inventory[].guid", errors),
                FileId = ReadOptionalInt(entry, "inventory[].fileId", errors),
                EmptyConfirmed = ReadOptionalBool(entry, "inventory[].emptyConfirmed", errors),
                Name = ReadString(entry, "inventory[].name", errors) ?? string.Empty,
                OwnershipValidated = ReadBool(entry, "inventory[].ownershipValidated", errors),
                Action = ReadEnum<JournalInventoryAction>(entry, "inventory[].action", errors),
                PhysicalState = ReadEnum<JournalPhysicalState>(entry, "inventory[].physicalState", errors),
                Confirmation = ReadEnum<JournalConfirmation>(entry, "inventory[].confirmation", errors),
                ExpectedHash = ReadOptionalString(entry, "inventory[].expectedHash", errors),
            };

            if (entry["composite"] is JObject composite)
            {
                item.Composite = new ApiPlanOperationJournalCompositeIdentity
                {
                    ExactName = ReadString(composite, "composite.exactName", errors) ?? string.Empty,
                    ObjectTypeName = ReadString(composite, "composite.objectTypeName", errors) ?? string.Empty,
                    Role = ReadString(composite, "composite.role", errors) ?? string.Empty,
                    CanonicalDescription = ReadString(composite, "composite.canonicalDescription", errors) ?? string.Empty,
                    TransactionGuid = ReadGuid(composite, "composite.transactionGuid", errors),
                    ApiGuid = ReadGuid(composite, "composite.apiGuid", errors),
                };
            }
            else if (entry["composite"] is not null && entry["composite"]!.Type != JTokenType.Null)
            {
                errors.Add("inventory[].composite deve ser objeto ou null.");
            }

            if (entry["receiptSequences"] is not JArray sequences)
            {
                errors.Add("inventory[].receiptSequences é obrigatório e deve ser um array.");
            }
            else
            {
                foreach (var sequence in sequences)
                {
                    if (sequence.Type != JTokenType.Integer)
                    {
                        errors.Add("inventory[].receiptSequences só aceita inteiros.");
                        continue;
                    }

                    item.ReceiptSequences.Add(sequence.Value<int>());
                }
            }

            journal.Inventory.Add(item);
        }
    }

    private static void ReadReceipts(JObject root, ApiPlanOperationJournal journal, List<string> errors)
    {
        if (root["receipts"] is not JArray receipts)
        {
            errors.Add("receipts é obrigatório e deve ser um array.");
            return;
        }

        foreach (var token in receipts)
        {
            if (token is not JObject entry)
            {
                errors.Add("receipts só aceita objetos.");
                continue;
            }

            journal.Receipts.Add(new ApiPlanOperationJournalReceipt
            {
                Sequence = ReadInt(entry, "receipts[].sequence", errors) ?? 0,
                Operation = ReadEnum<JournalReceiptOperation>(entry, "receipts[].operation", errors),
                Stage = ReadString(entry, "receipts[].stage", errors) ?? string.Empty,
                ObjectType = ReadEnum<JournalObjectType>(entry, "receipts[].objectType", errors),
                Attempt = ReadInt(entry, "receipts[].attempt", errors) ?? 0,
                RetryOfSequence = ReadOptionalInt(entry, "receipts[].retryOfSequence", errors),
                StartedUtc = ReadUtc(entry, "receipts[].startedUtc", errors),
                EndedUtc = ReadOptionalUtc(entry, "receipts[].endedUtc", errors),
                DurationMs = ReadLong(entry, "receipts[].durationMs", errors),
                AttemptState = ReadEnum<JournalAttemptState>(entry, "receipts[].attemptState", errors),
                Result = ReadEnum<JournalResult>(entry, "receipts[].result", errors),
                Confirmation = ReadEnum<JournalConfirmation>(entry, "receipts[].confirmation", errors),
                PhysicalState = ReadEnum<JournalPhysicalState>(entry, "receipts[].physicalState", errors),
                RetryEligible = ReadBool(entry, "receipts[].retryEligible", errors),
                RetryableReason = ReadOptionalEnum<JournalRetryableReason>(entry, "receipts[].retryableReason", errors),
            });
        }
    }

    private static void ReadAbandonment(JObject root, ApiPlanOperationJournal journal, List<string> errors)
    {
        var token = root["abandonment"];
        if (token is null || token.Type == JTokenType.Null)
        {
            journal.Abandonment = null;
            return;
        }

        if (token is not JObject entry)
        {
            errors.Add("abandonment deve ser objeto ou null.");
            return;
        }

        journal.Abandonment = new ApiPlanOperationJournalAbandonment
        {
            Reason = ReadString(entry, "abandonment.reason", errors) ?? string.Empty,
            AuthorizedUtc = ReadUtc(entry, "abandonment.authorizedUtc", errors),
            AuthorizedBy = ReadString(entry, "abandonment.authorizedBy", errors) ?? string.Empty,
        };
    }

    private static string? ReadString(JObject owner, string name, List<string> errors)
    {
        var token = owner[Leaf(name)];
        if (token is null || token.Type != JTokenType.String || string.IsNullOrWhiteSpace(token.Value<string>()))
        {
            errors.Add(name + " é obrigatório e deve ser uma string não vazia.");
            return null;
        }

        return token.Value<string>();
    }

    private static string? ReadOptionalString(JObject owner, string name, List<string> errors)
    {
        var token = owner[Leaf(name)];
        if (token is null || token.Type == JTokenType.Null)
        {
            return null;
        }

        if (token.Type != JTokenType.String || string.IsNullOrWhiteSpace(token.Value<string>()))
        {
            errors.Add(name + " deve ser uma string não vazia ou null.");
            return null;
        }

        return token.Value<string>();
    }

    private static int? ReadInt(JObject owner, string name, List<string> errors)
    {
        var token = owner[Leaf(name)];
        if (token is null || token.Type != JTokenType.Integer)
        {
            errors.Add(name + " é obrigatório e deve ser inteiro.");
            return null;
        }

        return token.Value<int>();
    }

    private static int? ReadOptionalInt(JObject owner, string name, List<string> errors)
    {
        var token = owner[Leaf(name)];
        if (token is null || token.Type == JTokenType.Null)
        {
            return null;
        }

        if (token.Type != JTokenType.Integer)
        {
            errors.Add(name + " deve ser inteiro ou null.");
            return null;
        }

        return token.Value<int>();
    }

    private static bool ReadBool(JObject owner, string name, List<string> errors)
    {
        var token = owner[Leaf(name)];
        if (token is null || token.Type != JTokenType.Boolean)
        {
            errors.Add(name + " é obrigatório e deve ser booleano.");
            return false;
        }

        return token.Value<bool>();
    }

    private static bool? ReadOptionalBool(JObject owner, string name, List<string> errors)
    {
        var token = owner[Leaf(name)];
        if (token is null || token.Type == JTokenType.Null)
        {
            return null;
        }

        if (token.Type != JTokenType.Boolean)
        {
            errors.Add(name + " deve ser booleano ou null.");
            return null;
        }

        return token.Value<bool>();
    }

    private static Guid ReadGuid(JObject owner, string name, List<string> errors)
    {
        var token = owner[Leaf(name)];
        if (token is null || token.Type != JTokenType.String || !Guid.TryParse(token.Value<string>(), out var value))
        {
            errors.Add(name + " é obrigatório e deve ser um GUID.");
            return Guid.Empty;
        }

        return value;
    }

    private static Guid? ReadOptionalGuid(JObject owner, string name, List<string> errors)
    {
        var token = owner[Leaf(name)];
        if (token is null || token.Type == JTokenType.Null)
        {
            return null;
        }

        if (token.Type != JTokenType.String || !Guid.TryParse(token.Value<string>(), out var value))
        {
            errors.Add(name + " deve ser um GUID ou null.");
            return null;
        }

        return value;
    }

    private static DateTime ReadUtc(JObject owner, string name, List<string> errors)
    {
        var token = owner[Leaf(name)];
        if (token is null || token.Type != JTokenType.String)
        {
            errors.Add(name + " é obrigatório e deve ser um timestamp UTC.");
            return default;
        }

        var text = token.Value<string>() ?? string.Empty;
        if (!DateTime.TryParseExact(
                text,
                UtcFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var value))
        {
            errors.Add(name + " deve seguir yyyy-MM-ddTHH:mm:ss.fffZ.");
            return default;
        }

        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static DateTime? ReadOptionalUtc(JObject owner, string name, List<string> errors)
    {
        var token = owner[Leaf(name)];
        if (token is null || token.Type == JTokenType.Null)
        {
            return null;
        }

        if (token.Type != JTokenType.String)
        {
            errors.Add(name + " deve ser um timestamp UTC ou null.");
            return null;
        }

        var text = token.Value<string>() ?? string.Empty;
        if (!DateTime.TryParseExact(
                text,
                UtcFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var value))
        {
            errors.Add(name + " deve seguir yyyy-MM-ddTHH:mm:ss.fffZ ou null.");
            return null;
        }

        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static long ReadLong(JObject owner, string name, List<string> errors)
    {
        var token = owner[Leaf(name)];
        if (token is null || token.Type != JTokenType.Integer)
        {
            errors.Add(name + " é obrigatório e deve ser inteiro.");
            return 0L;
        }

        return token.Value<long>();
    }

    private static TEnum ReadEnum<TEnum>(JObject owner, string name, List<string> errors)
        where TEnum : struct, Enum
    {
        var token = owner[Leaf(name)];
        if (token is null || token.Type != JTokenType.String
            || !Enum.TryParse<TEnum>(token.Value<string>(), ignoreCase: false, out var value)
            || !Enum.IsDefined(typeof(TEnum), value))
        {
            errors.Add(name + " é obrigatório e deve ser um valor conhecido de " + typeof(TEnum).Name + ".");
            return default;
        }

        return value;
    }

    private static TEnum? ReadOptionalEnum<TEnum>(JObject owner, string name, List<string> errors)
        where TEnum : struct, Enum
    {
        var token = owner[Leaf(name)];
        if (token is null || token.Type == JTokenType.Null)
        {
            return null;
        }

        if (token.Type != JTokenType.String
            || !Enum.TryParse<TEnum>(token.Value<string>(), ignoreCase: false, out var value)
            || !Enum.IsDefined(typeof(TEnum), value))
        {
            errors.Add(name + " deve ser um valor conhecido de " + typeof(TEnum).Name + " ou null.");
            return null;
        }

        return value;
    }

    /// <summary>
    /// Último segmento de um caminho qualificado: os leitores recebem <c>plan.services</c> ou
    /// <c>composite.role</c> para que a mensagem de erro diga onde o problema está, mas a
    /// propriedade JSON é sempre o nome simples.
    /// </summary>
    private static string Leaf(string name)
    {
        var separator = name.LastIndexOf('.');
        return separator < 0 ? name : name.Substring(separator + 1);
    }

    private static string Clean(string value) => (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
}

public sealed class ApiPlanOperationJournalReadResult
{
    private ApiPlanOperationJournalReadResult(
        ApiPlanOperationJournal? journal,
        string canonicalJson,
        IReadOnlyList<string> errors)
    {
        Journal = journal;
        CanonicalJson = canonicalJson;
        Errors = errors;
    }

    public ApiPlanOperationJournal? Journal { get; }

    /// <summary>Material canônico reserializado, vazio quando a leitura falhou.</summary>
    public string CanonicalJson { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Journal is not null && Errors.Count == 0;

    /// <summary>
    /// Hash do snapshot lido. Vazio quando a leitura falhou: um diário que não pôde ser
    /// validado não tem identidade de snapshot a comparar.
    /// </summary>
    public string SnapshotHash => IsValid
        ? ApiPlanOperationJournalSerializer.ComputeSnapshotHash(CanonicalJson)
        : string.Empty;

    public string Describe() => string.Join(" ", Errors);

    internal static ApiPlanOperationJournalReadResult Succeeded(ApiPlanOperationJournal journal, string canonicalJson) =>
        new ApiPlanOperationJournalReadResult(journal, canonicalJson, Array.Empty<string>());

    internal static ApiPlanOperationJournalReadResult Failed(string error) =>
        new ApiPlanOperationJournalReadResult(null, string.Empty, new[] { error });

    internal static ApiPlanOperationJournalReadResult Failed(IEnumerable<string> errors) =>
        new ApiPlanOperationJournalReadResult(null, string.Empty, errors.ToArray());
}
