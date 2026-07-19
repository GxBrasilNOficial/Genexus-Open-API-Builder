using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Artech.Architecture.Common.Objects;
using Artech.Common;
using Artech.Genexus.Common;
using Artech.Genexus.Common.Wiki;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Sonda temporaria B006. Criacao e exclusao exigem comandos explicitos.
/// Todas as demais fases sao somente leitura.
/// </summary>
internal static class MetadataFilePersistenceProbe
{
    private const string FileName = "fileGxOpenApiB006MetadataProbe.json";
    private const string Description = "Gx Open API Builder B006 Metadata File Probe";
    private const string ExpectedJson = "{\n"
        + "  \"schemaVersion\": 1,\n"
        + "  \"probe\": \"B006\",\n"
        + "  \"purpose\": \"persistencia de metadata em File\",\n"
        + "  \"encoding\": \"UTF-8\",\n"
        + "  \"unicode\": \"a\u00e7ao, cora\u00e7ao, n\u00f1, \u4e2d\",\n"
        + "  \"objects\": [{ \"kind\": \"File\", \"name\": \"fileGxOpenApiB006MetadataProbe.json\", \"owned\": true }],\n"
        + "  \"flags\": { \"reopenRequired\": true, \"conservative\": true }\n"
        + "}\n";

    public static string Preflight(KBModel designModel)
    {
        ValidateDesignModel(designModel);
        var files = FindFiles(designModel);
        return files.Length == 0
            ? "Pre-verificacao aprovada: nome B006 disponivel. Nenhuma alteracao foi feita."
            : $"Pre-verificacao bloqueada: encontrados {files.Length} File(s) com o nome B006. Nenhuma alteracao foi feita.";
    }

    public static string Create(KBModel designModel)
    {
        ValidateDesignModel(designModel);
        if (FindFiles(designModel).Length != 0)
        {
            throw new InvalidOperationException($"Criacao bloqueada: ja existe File chamado '{FileName}'. Nenhuma alteracao foi feita.");
        }

        var file = new WikiFileKBObject(designModel)
        {
            Name = FileName,
            Description = Description,
        };
        file.BlobPart.Data = BinaryStream.FromBytes(ExpectedBytes);
        file.Save();

        return DescribeVerifiedFile(designModel, "criado e relido imediatamente");
    }

    public static string ReadBeforeReopen(KBModel designModel)
    {
        return DescribeVerifiedFile(designModel, "relido antes do fechamento");
    }

    public static string ReadAfterReopen(KBModel designModel)
    {
        return DescribeVerifiedFile(designModel, "relido apos fechar e reabrir a KB");
    }

    public static string Delete(KBModel designModel)
    {
        var file = GetVerifiedFile(designModel);
        var guid = file.Guid;
        file.Delete();

        if (FindFiles(designModel).Length != 0)
        {
            throw new InvalidOperationException($"Exclusao do File '{FileName}' nao foi confirmada.");
        }

        return $"File B006 excluido e ausencia confirmada: Guid='{guid}'.";
    }

    private static byte[] ExpectedBytes => Encoding.UTF8.GetBytes(ExpectedJson);

    private static WikiFileKBObject[] FindFiles(KBModel designModel)
    {
        return WikiFileKBObject.GetAll(designModel)
            .Where(item => string.Equals(item.Name, FileName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static WikiFileKBObject GetVerifiedFile(KBModel designModel)
    {
        ValidateDesignModel(designModel);
        var files = FindFiles(designModel);
        if (files.Length != 1)
        {
            throw new InvalidOperationException($"Era esperado exatamente um File B006 chamado '{FileName}', mas foram encontrados {files.Length}. Nenhuma alteracao foi feita.");
        }

        var file = files[0];
        if (!string.Equals(file.Name, FileName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("O nome do File nao preservou a caixa esperada. Nenhuma alteracao foi feita.");
        }

        if (!string.Equals(file.Description, Description, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A descricao sentinela B006 nao confere. Nenhuma alteracao foi feita.");
        }

        var actualBytes = file.BlobPart?.Data?.GetBytes();
        if (actualBytes is null)
        {
            throw new InvalidOperationException("O File B006 nao possui conteudo binario. Nenhuma alteracao foi feita.");
        }

        if (!actualBytes.SequenceEqual(ExpectedBytes))
        {
            throw new InvalidOperationException("O File B006 nao preservou exatamente os bytes UTF-8 esperados. Nenhuma alteracao foi feita.");
        }

        var actualText = Encoding.UTF8.GetString(actualBytes);
        if (!string.Equals(actualText, ExpectedJson, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A releitura textual UTF-8 divergiu do JSON esperado. Nenhuma alteracao foi feita.");
        }

        return file;
    }

    private static string DescribeVerifiedFile(KBModel designModel, string phase)
    {
        var file = GetVerifiedFile(designModel);
        var bytes = file.BlobPart.Data.GetBytes();
        return $"File B006 {phase}: Name='{file.Name}', Guid='{file.Guid}', Description='{file.Description}', Bytes={bytes.Length}, Sha256='{ComputeSha256(bytes)}'.";
    }

    private static string ComputeSha256(byte[] bytes)
    {
        using (var algorithm = SHA256.Create())
        {
            return BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", string.Empty);
        }
    }

    private static void ValidateDesignModel(KBModel designModel)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }
    }
}
