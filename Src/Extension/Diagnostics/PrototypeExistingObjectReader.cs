using System;
using System.Collections.Generic;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Wiki;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Verifica, em modo somente leitura, os nomes planejados para a API derivada
/// da Transaction selecionada no prototipo navegavel.
/// </summary>
internal static class PrototypeExistingObjectReader
{
    private static readonly string[] ProcedureSuffixes = { "List", "Get", "Create", "Update", "Delete" };

    private static readonly string[] SdtSuffixes =
    {
        "CreateRequest",
        "UpdateRequest",
        "Response",
        "ListFilters",
        "ListResponse",
    };

    public static PrototypeExistingObjectsSnapshot Read(KBModel designModel, Transaction transaction)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        if (transaction is null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        var baseName = transaction.Name;
        var results = new List<PrototypeExistingObjectResult>
        {
            CreateResult("API", $"api{baseName}", CountByName(API.GetAll(designModel), $"api{baseName}")),
            CreateResult("File", $"api{baseName}_Metadata", CountByName(WikiFileKBObject.GetAll(designModel), $"api{baseName}_Metadata")),
            CreateResult("Folder", $"{baseName}OpenApi", CountByName(Folder.GetAll(designModel), $"{baseName}OpenApi")),
        };

        foreach (var suffix in ProcedureSuffixes)
        {
            var name = $"proc{baseName}_API_{suffix}";
            results.Add(CreateResult("Procedure", name, CountByName(Procedure.GetAll(designModel), name)));
        }

        foreach (var suffix in SdtSuffixes)
        {
            var name = $"sdt{baseName}_API_{suffix}";
            results.Add(CreateResult("SDT", name, CountByName(SDT.GetAll(designModel), name)));
        }

        results.Add(CreateResult("Folder", "GxOpenAPI", CountByName(Folder.GetAll(designModel), "GxOpenAPI")));
        results.Add(CreateResult("SDT", "sdt_API_ErrorMessage", CountByName(SDT.GetAll(designModel), "sdt_API_ErrorMessage")));
        results.Add(CreateResult("SDT", "sdt_API_ErrorResponse", CountByName(SDT.GetAll(designModel), "sdt_API_ErrorResponse")));
        results.Add(CreateResult("SDT", "sdt_API_Pagination", CountByName(SDT.GetAll(designModel), "sdt_API_Pagination")));

        return new PrototypeExistingObjectsSnapshot(baseName, $"api{baseName}_Metadata", results);
    }

    private static int CountByName<T>(IEnumerable<T> objects, string name)
        where T : KBObject
    {
        return objects.Count(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static PrototypeExistingObjectResult CreateResult(string objectType, string name, int count)
    {
        return new PrototypeExistingObjectResult(objectType, name, count, DescribeStatus(count));
    }

    private static string DescribeStatus(int count)
    {
        if (count == 0)
        {
            return "Ausente";
        }

        return count == 1 ? "Existente" : "Multiplo";
    }
}

internal sealed class PrototypeExistingObjectsSnapshot
{
    public PrototypeExistingObjectsSnapshot(string transactionName, string metadataFileName, IReadOnlyList<PrototypeExistingObjectResult> results)
    {
        TransactionName = transactionName ?? throw new ArgumentNullException(nameof(transactionName));
        MetadataFileName = metadataFileName ?? throw new ArgumentNullException(nameof(metadataFileName));
        Results = results ?? throw new ArgumentNullException(nameof(results));
    }

    public string TransactionName { get; }

    public string MetadataFileName { get; }

    public IReadOnlyList<PrototypeExistingObjectResult> Results { get; }

    public int ExistingCount => Results.Count(result => result.Count > 0);

    public int MissingCount => Results.Count(result => result.Count == 0);
}

internal sealed class PrototypeExistingObjectResult
{
    public PrototypeExistingObjectResult(string objectType, string name, int count, string status)
    {
        ObjectType = objectType ?? throw new ArgumentNullException(nameof(objectType));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Count = count;
        Status = status ?? throw new ArgumentNullException(nameof(status));
    }

    public string ObjectType { get; }

    public string Name { get; }

    public int Count { get; }

    public string Status { get; }
}
