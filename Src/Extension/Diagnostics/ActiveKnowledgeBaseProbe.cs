using System;
using Artech.Architecture.Common.Objects;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Consulta a Knowledge Base recebida pelo evento público de abertura da IDE.
/// Não abre, cria, salva, fecha ou altera objetos GeneXus.
/// </summary>
internal static class ActiveKnowledgeBaseProbe
{
    public static ActiveKnowledgeBaseSnapshot? TryRead(KnowledgeBase? knowledgeBase)
    {
        if (knowledgeBase is null)
        {
            return null;
        }

        return new ActiveKnowledgeBaseSnapshot(
            knowledgeBase.Name,
            knowledgeBase.Guid.ToString(),
            Convert.ToString(knowledgeBase.Location) ?? string.Empty);
    }
}

/// <summary>
/// Dados observados da Knowledge Base ativa, preservados apenas em memória.
/// </summary>
internal sealed class ActiveKnowledgeBaseSnapshot
{
    public ActiveKnowledgeBaseSnapshot(string name, string guid, string location)
    {
        Name = name;
        Guid = guid;
        Location = location;
    }

    public string Name { get; }

    public string Guid { get; }

    public string Location { get; }
}