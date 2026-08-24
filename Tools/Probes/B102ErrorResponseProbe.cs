// SONDA DORMENTE — NÃO COMPILADA.
//
// Este arquivo está em Tools/Probes/, fora da árvore compilada da extensão. O
// .csproj de Src/Extension usa globs padrão do SDK e inclui explicitamente
// ..\Domain\**\*.cs; nenhum dos dois alcança Tools/. Portanto esta classe não
// entra na DLL publicada e não pode ser invocada em runtime.
//
// Preservada por valor documental, conforme a seção "Fechamento de spikes e
// sondas temporárias" do AGENTS.md: é o único código do repositório que sabe
// interrogar o SDK sobre criação de SDT, membro coleção tipado por SDT separado
// e normalização de Length. Serve de ponto de partida para experimentos da mesma
// família, como o B101 (membro nullable).
//
// Executada uma vez em 2026-08-24, numa KB de teste, para fechar os dois gates
// humanos do B102. Resultado e análise em
// Docs/Implementation/2026-08-24-B102-EXPERIMENTO-E-GATE-HTTP.md.
//
// Para reutilizar: copiar para Src/Extension/Diagnostics/, registrar os comandos
// nas três camadas (Package.cs, GenexusOpenApiBuilder.package e o grupo de menu),
// compilar e reinstalar. A reativação é deliberada por construção — não acontece
// por acidente. Ao terminar, desfazer o registro e trazer o arquivo de volta para
// cá. Note que ResultPathCandidates tem um caminho absoluto da máquina de origem.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Parts.SDT;
using Artech.Genexus.Common.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Sonda temporária B102: verifica na IDE se a coleção Messages tipada por SDT
/// separado é aceita, e qual Length o SDK devolve após Save+releitura para
/// membros LongVarChar (valor normalizado, não o solicitado na criação).
/// </summary>
internal static class B102ErrorResponseProbe
{
    internal const string ErrorMessageSdtName = "sdt_GOAB_B102_ErrorMessage";
    internal const string ErrorResponseSdtName = "sdt_GOAB_B102_ErrorResponse";
    internal const string LengthSdtNamePrefix = "sdt_GOAB_B102_Len_";
    private const string DescriptionPrefix = "Gx Open API Builder B102 Probe";
    private const string ResultFileName = "b102-probe-result.json";

    private static readonly int[] LengthRequests = { 0, 2048, 1048576, 2097152 };

    private static readonly string[] ResultPathCandidates =
    {
        @"C:\Dev\Knowledge\Genexus-Open-API-Builder\Temp\b102-probe-result.json",
        Path.Combine(Path.GetTempPath(), "goab-b102-probe-result.json"),
    };

    public static string Run(KBModel designModel)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        EnsureProbeNamesAvailable(designModel);

        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz");
        var collection = RunCollectionExperiment(designModel);
        var lengths = RunLengthExperiments(designModel);

        var document = new JObject
        {
            ["probe"] = "B102",
            ["timestamp"] = timestamp,
            ["kbModelGuid"] = designModel.Guid.ToString("D"),
            ["collectionExperiment"] = collection,
            ["longVarCharLengthExperiments"] = new JArray(lengths),
            ["notes"] = new JArray(
                "lengthObserved is SDTItem.Length after Save + reload by GUID; not the value passed to AddItem.",
                "collectionAccepted reflects whether ErrorResponse with Messages collection typed by separate SDT saved successfully."),
        };

        var resultPath = WriteResult(document);
        return $"[Genexus Open API Builder][B102] Sonda concluida. Resultado em '{resultPath}'. collectionAccepted={collection["accepted"]}, lengthAttempts={lengths.Count}.";
    }

    public static string Cleanup(KBModel designModel)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        var targets = FindProbeSdts(designModel).ToArray();
        if (targets.Length == 0)
        {
            return "[Genexus Open API Builder][B102] Limpeza: nenhum SDT de sonda B102 encontrado. Nenhuma alteracao foi feita.";
        }

        // ErrorResponse referencia ErrorMessage; apagar primeiro o consumidor.
        foreach (var sdt in targets.OrderByDescending(item => string.Equals(item.Name, ErrorResponseSdtName, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                     .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            var name = sdt.Name;
            sdt.Delete();
            if (SDT.GetAll(designModel).Any(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Limpeza B102 falhou: SDT '{name}' ainda existe apos Delete().");
            }
        }

        var remaining = FindProbeSdts(designModel).Select(item => item.Name).ToArray();
        if (remaining.Length > 0)
        {
            throw new InvalidOperationException($"Limpeza B102 incompleta: ainda restam {string.Join(", ", remaining)}.");
        }

        return $"[Genexus Open API Builder][B102] Limpeza concluida: Deleted={targets.Length}.";
    }

    private static JObject RunCollectionExperiment(KBModel designModel)
    {
        var result = new JObject
        {
            ["errorMessageSdt"] = ErrorMessageSdtName,
            ["errorResponseSdt"] = ErrorResponseSdtName,
            ["accepted"] = false,
            ["stage"] = "not_started",
            ["error"] = null,
            ["errorMessageMembers"] = null,
            ["errorResponseMembers"] = null,
        };

        try
        {
            result["stage"] = "create_error_message";
            var messageSdt = new SDT(designModel)
            {
                Name = ErrorMessageSdtName,
                Description = $"{DescriptionPrefix} - ErrorMessage",
            };
            var messageRoot = messageSdt.SDTStructure.Root;
            messageRoot.Name = ErrorMessageSdtName;
            messageRoot.Items.Clear();
            messageRoot.AddItem("Code", eDBType.VARCHAR, 64, 0);
            messageRoot.AddItem("Message", eDBType.LONGVARCHAR, 0, 0);
            messageSdt.Save();

            var reloadedMessage = SDT.Get(designModel, messageSdt.Guid)
                ?? throw new InvalidOperationException($"SDT '{ErrorMessageSdtName}' nao foi reencontrado apos Save().");
            result["errorMessageMembers"] = ReadMembers(reloadedMessage);

            result["stage"] = "create_error_response_with_messages_collection";
            var responseSdt = new SDT(designModel)
            {
                Name = ErrorResponseSdtName,
                Description = $"{DescriptionPrefix} - ErrorResponse",
            };
            var responseRoot = responseSdt.SDTStructure.Root;
            responseRoot.Name = ErrorResponseSdtName;
            responseRoot.Items.Clear();
            responseRoot.AddItem("Code", eDBType.VARCHAR, 64, 0);
            responseRoot.AddItem("Message", eDBType.LONGVARCHAR, 0, 0);

            var messages = responseRoot.AddItem("Messages", eDBType.GX_SDT);
            if (!DataType.ParseInto(designModel, ErrorMessageSdtName, messages))
            {
                throw new InvalidOperationException($"Tipo SDT '{ErrorMessageSdtName}' nao resolvido para membro Messages.");
            }

            messages.IsCollection = true;
            messages.CollectionItemName = ErrorMessageSdtName;
            responseSdt.Save();

            var reloadedResponse = SDT.Get(designModel, responseSdt.Guid)
                ?? throw new InvalidOperationException($"SDT '{ErrorResponseSdtName}' nao foi reencontrado apos Save().");
            result["errorResponseMembers"] = ReadMembers(reloadedResponse);
            result["accepted"] = true;
            result["stage"] = "completed";
        }
        catch (Exception ex)
        {
            result["accepted"] = false;
            result["error"] = ex.Message;
        }

        return result;
    }

    private static List<JObject> RunLengthExperiments(KBModel designModel)
    {
        var results = new List<JObject>();
        foreach (var requested in LengthRequests)
        {
            var sdtName = LengthSdtNamePrefix + requested;
            var entry = new JObject
            {
                ["sdtName"] = sdtName,
                ["memberName"] = "Message",
                ["lengthRequested"] = requested,
                ["accepted"] = false,
                ["lengthObserved"] = null,
                ["typeObserved"] = null,
                ["error"] = null,
            };

            try
            {
                var sdt = new SDT(designModel)
                {
                    Name = sdtName,
                    Description = $"{DescriptionPrefix} - Len {requested}",
                };
                var root = sdt.SDTStructure.Root;
                root.Name = sdtName;
                root.Items.Clear();
                root.AddItem("Message", eDBType.LONGVARCHAR, requested, 0);
                sdt.Save();

                var reloaded = SDT.Get(designModel, sdt.Guid)
                    ?? throw new InvalidOperationException($"SDT '{sdtName}' nao foi reencontrado apos Save().");
                var message = FindMember(reloaded, "Message")
                    ?? throw new InvalidOperationException($"Membro 'Message' ausente em '{sdtName}' apos releitura.");

                entry["accepted"] = true;
                entry["lengthObserved"] = message.Length;
                entry["typeObserved"] = message.Type.ToString();
            }
            catch (Exception ex)
            {
                entry["accepted"] = false;
                entry["error"] = ex.Message;
            }

            results.Add(entry);
        }

        return results;
    }

    private static JArray ReadMembers(SDT sdt)
    {
        var members = new JArray();
        foreach (SDTItem item in sdt.SDTStructure.Root.Items)
        {
            members.Add(new JObject
            {
                ["name"] = item.Name,
                ["type"] = item.Type.ToString(),
                ["lengthObserved"] = item.Length,
                ["decimalsObserved"] = item.Decimals,
                ["isCollection"] = item.IsCollection,
                ["collectionItemName"] = item.CollectionItemName,
            });
        }

        return members;
    }

    private static SDTItem? FindMember(SDT sdt, string memberName)
    {
        foreach (SDTItem item in sdt.SDTStructure.Root.Items)
        {
            if (string.Equals(item.Name, memberName, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }

    private static void EnsureProbeNamesAvailable(KBModel designModel)
    {
        var existing = FindProbeSdts(designModel).Select(item => item.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
        if (existing.Length > 0)
        {
            throw new InvalidOperationException(
                $"Sonda B102 bloqueada: ja existem SDTs de sonda ({string.Join(", ", existing)}). Execute 'Limpar sonda B102' antes. Nenhuma alteracao foi feita.");
        }
    }

    private static IEnumerable<SDT> FindProbeSdts(KBModel designModel)
    {
        return SDT.GetAll(designModel)
            .Where(IsProbeSdt);
    }

    private static bool IsProbeSdt(SDT sdt)
    {
        if (string.Equals(sdt.Name, ErrorMessageSdtName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sdt.Name, ErrorResponseSdtName, StringComparison.OrdinalIgnoreCase) ||
            sdt.Name.StartsWith(LengthSdtNamePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(sdt.Description) &&
               sdt.Description.StartsWith(DescriptionPrefix, StringComparison.Ordinal);
    }

    private static string WriteResult(JObject document)
    {
        Exception? lastError = null;
        foreach (var path in ResultPathCandidates)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, document.ToString(Formatting.Indented) + Environment.NewLine);
                return path;
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        throw new InvalidOperationException(
            $"Sonda B102 nao conseguiu gravar o JSON de resultado. Ultimo erro: {lastError?.Message}");
    }
}
