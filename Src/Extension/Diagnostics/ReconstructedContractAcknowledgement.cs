#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>B110: aceite do conjunto vindo da KB; edição não remove seção e não há hash de valores.</summary>
public sealed class ReconstructedContractAcknowledgement
{
    public ReconstructedContractAcknowledgement(IEnumerable<string> sections, IEnumerable<string>? editedSections = null)
    {
        Sections = Array.AsReadOnly(sections.Distinct(StringComparer.Ordinal).OrderBy(key => key, StringComparer.Ordinal).ToArray());
        EditedSections = Array.AsReadOnly((editedSections ?? Array.Empty<string>())
            .Where(key => Sections.Contains(key, StringComparer.Ordinal)).Distinct(StringComparer.Ordinal).ToArray());
    }
    public IReadOnlyList<string> Sections { get; }
    public IReadOnlyList<string> EditedSections { get; }

    public static bool IsAccepted(bool imported, bool hasLevelsKey, bool hasSublevels,
        IEnumerable<string> recalculatedSections, ReconstructedContractAcknowledgement? acknowledgement)
    {
        if (!imported) return true;
        if (IsHierarchyBlocked(imported, hasLevelsKey, hasSublevels) || acknowledgement is null) return false;
        return new HashSet<string>(recalculatedSections, StringComparer.Ordinal).SetEquals(acknowledgement.Sections);
    }
    public static bool IsHierarchyBlocked(bool imported, bool hasLevelsKey, bool hasSublevels)
        => imported && !hasLevelsKey && hasSublevels;

    public const string HierarchyBlocked = "Contrato reconstruído bloqueado: a Transaction tem subníveis e a metadata recuperada não contém levels. Use Remover API gerada e depois gere novamente pelo Wizard. Nenhuma escrita foi solicitada.";
    public const string ConfirmationRequired = "Contrato reconstruído bloqueado: confirme as seções recuperadas no Wizard; a confirmação deve corresponder ao conjunto recalculado pela KB. Nenhuma escrita foi solicitada.";
}
