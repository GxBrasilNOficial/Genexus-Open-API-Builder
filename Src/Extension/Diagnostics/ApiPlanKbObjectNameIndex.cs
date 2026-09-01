using System;
using System.Collections.Generic;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Wiki;
using GxAttribute = Artech.Genexus.Common.Objects.Attribute;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Índice por nome dos objetos da KB — uma varredura GetAll por tipo (B082 / abertura do wizard).
/// Reutilizar entre inspeção do wizard e Apply evita O(n×m) no preflight de KBs grandes.
/// </summary>
internal sealed class ApiPlanKbObjectNameIndex
{
    private readonly ILookup<string, Folder> _folders;
    private ILookup<string, SDT> _sdts;
    private readonly ILookup<string, Procedure> _procedures;
    private readonly ILookup<string, API> _apis;
    private readonly ILookup<string, WikiFileKBObject> _files;
    private readonly ILookup<string, Transaction> _transactions;
    private readonly ILookup<string, GxAttribute> _attributes;

    private ApiPlanKbObjectNameIndex(
        ILookup<string, Folder> folders,
        ILookup<string, SDT> sdts,
        ILookup<string, Procedure> procedures,
        ILookup<string, API> apis,
        ILookup<string, WikiFileKBObject> files,
        ILookup<string, Transaction> transactions,
        ILookup<string, GxAttribute> attributes)
    {
        _folders = folders;
        _sdts = sdts;
        _procedures = procedures;
        _apis = apis;
        _files = files;
        _transactions = transactions;
        _attributes = attributes;
    }

    internal static ApiPlanKbObjectNameIndex Create(KBModel designModel, ApiPlanBusyProgressSession? progress = null)
    {
        progress?.Report("KB", 0, 0, "Indexando objetos");
        progress?.PumpAndThrowIfAbortRequested();

        var index = new ApiPlanKbObjectNameIndex(
            Folder.GetAll(designModel).ToLookup(item => item.Name, StringComparer.OrdinalIgnoreCase),
            SDT.GetAll(designModel).ToLookup(item => item.Name, StringComparer.OrdinalIgnoreCase),
            Procedure.GetAll(designModel).ToLookup(item => item.Name, StringComparer.OrdinalIgnoreCase),
            API.GetAll(designModel).ToLookup(item => item.Name, StringComparer.OrdinalIgnoreCase),
            WikiFileKBObject.GetAll(designModel).ToLookup(item => item.Name, StringComparer.OrdinalIgnoreCase),
            Transaction.GetAll(designModel).ToLookup(item => item.Name, StringComparer.OrdinalIgnoreCase),
            GxAttribute.GetAll(designModel).ToLookup(item => item.Name, StringComparer.OrdinalIgnoreCase));

        progress?.PumpAndThrowIfAbortRequested();
        return index;
    }

    /// <summary>
    /// Reindexa só SDTs após Save() — o índice pré-Apply não contém objetos recém-criados.
    /// </summary>
    internal void RefreshSdts(KBModel designModel)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }

        _sdts = SDT.GetAll(designModel).ToLookup(item => item.Name, StringComparer.OrdinalIgnoreCase);
    }

    internal IReadOnlyList<Folder> FindFolders(string name) => _folders[name].ToArray();

    internal IReadOnlyList<SDT> FindSdts(string name) => _sdts[name].ToArray();

    internal IReadOnlyList<Procedure> FindProcedures(string name) => _procedures[name].ToArray();

    internal IReadOnlyList<API> FindApis(string name) => _apis[name].ToArray();

    internal IReadOnlyList<WikiFileKBObject> FindFiles(string name) => _files[name].ToArray();

    internal IReadOnlyList<Transaction> FindTransactions(string name) => _transactions[name].ToArray();

    internal IReadOnlyList<GxAttribute> FindAttributes(string name) => _attributes[name].ToArray();

    internal int GetSdtCount(string name) => _sdts[name].Count();

    internal bool TryGetSingleSdt(string name, out SDT sdt)
    {
        using (var enumerator = _sdts[name].GetEnumerator())
        {
            if (!enumerator.MoveNext())
            {
                sdt = null!;
                return false;
            }

            sdt = enumerator.Current;
            if (enumerator.MoveNext())
            {
                sdt = null!;
                return false;
            }

            return true;
        }
    }

    internal bool TryGetSingleAttribute(string name, out GxAttribute attribute)
    {
        using (var enumerator = _attributes[name].GetEnumerator())
        {
            if (!enumerator.MoveNext())
            {
                attribute = null!;
                return false;
            }

            attribute = enumerator.Current;
            if (enumerator.MoveNext())
            {
                attribute = null!;
                return false;
            }

            return true;
        }
    }

    internal bool OwnedSdtExists(string name) =>
        TryGetSingleSdt(name, out var sdt) && ApiPlanOwnedObjectDescription.IsOwnedSdt(sdt.Description, name);
}
