using System;
using System.Linq;
using Artech.Architecture.Common.Objects;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Cria um único Folder de prova no Root Module quando ele ainda não existe.
/// Não altera nem substitui objetos existentes.
/// </summary>
internal static class FolderCreationProbe
{
    public static string CreateIfAbsent(KnowledgeBase knowledgeBase, string folderName)
    {
        if (knowledgeBase is null)
        {
            throw new ArgumentNullException(nameof(knowledgeBase));
        }

        if (string.IsNullOrWhiteSpace(folderName))
        {
            throw new ArgumentException("O nome do Folder é obrigatório.", nameof(folderName));
        }

        var existingFolder = Folder.GetAll(knowledgeBase.DesignModel)
            .FirstOrDefault(folder => string.Equals(folder.Name, folderName, StringComparison.OrdinalIgnoreCase));

        if (existingFolder is not null)
        {
            return $"Folder de teste já existente: {existingFolder.Name}. Nenhuma alteração foi feita.";
        }

        var folder = new Folder(knowledgeBase.DesignModel, folderName);
        folder.Save();
        return $"Folder de teste criado: {folder.Name}.";
    }
}