using System;
using System.Linq;
using System.Text;
using Artech.Architecture.Common.Objects;
using Artech.Common;
using Artech.Genexus.Common;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Wiki;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Executa o ciclo B005 somente por comandos explicitos. Cada fase deve ser
/// disparada manualmente e com autorizacao previa quando houver escrita na KB.
/// </summary>
internal static class B005LifecycleProbe
{
    private const string ProcedureName = "procGxOpenApiB005Probe";
    private const string SdtName = "sdtGxOpenApiB005Probe";
    private const string FolderName = "GxOpenApiB005ProbeFolder";
    private const string FileName = "fileGxOpenApiB005Probe.json";
    private const string InitialDescription = "Gx Open API Builder B005 Probe - criado";
    private const string UpdatedDescription = "Gx Open API Builder B005 Probe - alterado";
    private const string InitialJson = "{\"marker\":\"B005-criado\"}";
    private const string UpdatedJson = "{\"marker\":\"B005-alterado\"}";

    public static string Preflight(KBModel designModel)
    {
        ValidateDesignModel(designModel);

        var snapshot = ReadSnapshot(designModel);
        return snapshot.HasAny
            ? $"Pre-verificacao: nomes B005 indisponiveis: {snapshot.DescribeCounts()}. Nenhuma alteracao foi feita."
            : "Pre-verificacao: nomes B005 disponiveis para Procedure, SDT, Folder e File. Nenhuma alteracao foi feita.";
    }

    public static string Create(KBModel designModel)
    {
        ValidateDesignModel(designModel);
        EnsureNoProbeExists(designModel, "Criacao bloqueada");

        var folder = new Folder(designModel, FolderName)
        {
            Description = InitialDescription,
        };
        folder.Save();

        var procedure = new Procedure(designModel)
        {
            Name = ProcedureName,
            Description = InitialDescription,
        };
        procedure.Save();

        var sdt = new SDT(designModel)
        {
            Name = SdtName,
            Description = InitialDescription,
        };
        ConfigureSdt(sdt);
        sdt.Save();

        var file = new WikiFileKBObject(designModel)
        {
            Name = FileName,
            Description = InitialDescription,
        };
        file.BlobPart.Data = BinaryStream.FromBytes(Encoding.UTF8.GetBytes(InitialJson));
        file.Save();

        var created = GetVerifiedProbe(designModel, InitialDescription, InitialJson);
        return $"Objetos B005 criados e relidos: Procedure='{created.Procedure.Guid}', SDT='{created.Sdt.Guid}', Folder='{created.Folder.Guid}', File='{created.File.Guid}'.";
    }

    public static string Update(KBModel designModel)
    {
        var probe = GetVerifiedProbe(designModel, InitialDescription, InitialJson);

        probe.Folder.Description = UpdatedDescription;
        probe.Folder.Save();

        probe.Procedure.Description = UpdatedDescription;
        probe.Procedure.Save();

        probe.Sdt.Description = UpdatedDescription;
        probe.Sdt.Save();

        probe.File.Description = UpdatedDescription;
        probe.File.BlobPart.Data = BinaryStream.FromBytes(Encoding.UTF8.GetBytes(UpdatedJson));
        probe.File.Save();

        var updated = GetVerifiedProbe(designModel, UpdatedDescription, UpdatedJson);
        return $"Objetos B005 alterados e relidos: Procedure='{updated.Procedure.Guid}', SDT='{updated.Sdt.Guid}', Folder='{updated.Folder.Guid}', File='{updated.File.Guid}'.";
    }

    public static string Read(KBModel designModel)
    {
        var probe = GetVerifiedProbe(designModel, UpdatedDescription, UpdatedJson);
        return $"Objetos B005 relidos: Procedure='{probe.Procedure.Guid}', SDT='{probe.Sdt.Guid}', Folder='{probe.Folder.Guid}', File='{probe.File.Guid}'.";
    }

    public static string Delete(KBModel designModel)
    {
        ValidateDesignModel(designModel);

        var procedures = Procedure.GetAll(designModel)
            .Where(item => string.Equals(item.Name, ProcedureName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var sdts = SDT.GetAll(designModel)
            .Where(item => string.Equals(item.Name, SdtName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var folders = Folder.GetAll(designModel)
            .Where(item => string.Equals(item.Name, FolderName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var files = WikiFileKBObject.GetAll(designModel)
            .Where(item => string.Equals(item.Name, FileName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        EnsureAtMostOneOwned(procedures, "Procedure", ProcedureName);
        EnsureAtMostOneOwned(sdts, "SDT", SdtName);
        EnsureAtMostOneOwned(folders, "Folder", FolderName);
        EnsureAtMostOneOwned(files, "File", FileName);

        var procedureGuid = procedures.SingleOrDefault()?.Guid;
        var sdtGuid = sdts.SingleOrDefault()?.Guid;
        var folderGuid = folders.SingleOrDefault()?.Guid;
        var fileGuid = files.SingleOrDefault()?.Guid;

        foreach (var file in files)
        {
            file.Delete();
        }

        foreach (var sdt in sdts)
        {
            sdt.Delete();
        }

        foreach (var procedure in procedures)
        {
            procedure.Delete();
        }

        foreach (var folder in folders)
        {
            folder.Delete();
        }

        var stillExists = ReadSnapshot(designModel).HasAny;
        if (stillExists)
        {
            throw new InvalidOperationException("A exclusao dos objetos B005 nao foi confirmada.");
        }

        return $"Objetos B005 excluidos e ausencia confirmada: Procedure='{procedureGuid}', SDT='{sdtGuid}', Folder='{folderGuid}', File='{fileGuid}'.";
    }

    private static void ConfigureSdt(SDT sdt)
    {
        var root = sdt.SDTStructure.Root;
        root.Name = SdtName;
        root.AddItem("ProbeValue", eDBType.VARCHAR, 128, 0);
    }

    private static void EnsureNoProbeExists(KBModel designModel, string prefix)
    {
        var snapshot = ReadSnapshot(designModel);
        if (snapshot.HasAny)
        {
            throw new InvalidOperationException($"{prefix}: ja existem objetos com nomes B005: {snapshot.DescribeCounts()}. Nenhuma alteracao foi feita.");
        }
    }

    private static ProbeObjects GetVerifiedProbe(KBModel designModel, string expectedDescription, string expectedJson)
    {
        ValidateDesignModel(designModel);

        var procedures = Procedure.GetAll(designModel)
            .Where(item => string.Equals(item.Name, ProcedureName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var sdts = SDT.GetAll(designModel)
            .Where(item => string.Equals(item.Name, SdtName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var folders = Folder.GetAll(designModel)
            .Where(item => string.Equals(item.Name, FolderName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var files = WikiFileKBObject.GetAll(designModel)
            .Where(item => string.Equals(item.Name, FileName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        EnsureSingle(procedures.Length, "Procedure", ProcedureName);
        EnsureSingle(sdts.Length, "SDT", SdtName);
        EnsureSingle(folders.Length, "Folder", FolderName);
        EnsureSingle(files.Length, "File", FileName);

        var probe = new ProbeObjects(procedures[0], sdts[0], folders[0], files[0]);
        EnsureDescription(probe.Procedure.Description, expectedDescription, "Procedure", ProcedureName);
        EnsureDescription(probe.Sdt.Description, expectedDescription, "SDT", SdtName);
        EnsureDescription(probe.Folder.Description, expectedDescription, "Folder", FolderName);
        EnsureDescription(probe.File.Description, expectedDescription, "File", FileName);
        EnsureFileContent(probe.File, expectedJson);

        return probe;
    }

    private static ProbeSnapshot ReadSnapshot(KBModel designModel)
    {
        return new ProbeSnapshot(
            Procedure.GetAll(designModel).Count(item => string.Equals(item.Name, ProcedureName, StringComparison.OrdinalIgnoreCase)),
            SDT.GetAll(designModel).Count(item => string.Equals(item.Name, SdtName, StringComparison.OrdinalIgnoreCase)),
            Folder.GetAll(designModel).Count(item => string.Equals(item.Name, FolderName, StringComparison.OrdinalIgnoreCase)),
            WikiFileKBObject.GetAll(designModel).Count(item => string.Equals(item.Name, FileName, StringComparison.OrdinalIgnoreCase)));
    }

    private static void EnsureAtMostOneOwned<T>(T[] items, string objectType, string objectName)
        where T : KBObject
    {
        if (items.Length > 1)
        {
            throw new InvalidOperationException(
                $"Era esperado no maximo um objeto {objectType} chamado '{objectName}', mas foram encontrados {items.Length}. Nenhuma alteracao foi feita.");
        }

        if (items.Length == 1)
        {
            var description = items[0].Description;
            var hasExpectedDescription =
                string.Equals(description, InitialDescription, StringComparison.Ordinal) ||
                string.Equals(description, UpdatedDescription, StringComparison.Ordinal);
            if (!hasExpectedDescription)
            {
                throw new InvalidOperationException(
                    $"O objeto {objectType} '{objectName}' nao possui descricao sentinela do teste B005. Nenhuma alteracao foi feita.");
            }
        }
    }

    private static void EnsureSingle(int count, string objectType, string objectName)
    {
        if (count != 1)
        {
            throw new InvalidOperationException(
                $"Era esperado exatamente um objeto {objectType} chamado '{objectName}', mas foram encontrados {count}. Nenhuma alteracao foi feita.");
        }
    }

    private static void EnsureDescription(string actualDescription, string expectedDescription, string objectType, string objectName)
    {
        if (!string.Equals(actualDescription, expectedDescription, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"O objeto {objectType} '{objectName}' nao possui a descricao esperada do teste B005. Nenhuma alteracao foi feita.");
        }
    }

    private static void EnsureFileContent(WikiFileKBObject file, string expectedJson)
    {
        if (file.BlobPart?.Data is null)
        {
            throw new InvalidOperationException($"O File '{FileName}' nao possui conteudo binario esperado. Nenhuma alteracao foi feita.");
        }

        var actualJson = Encoding.UTF8.GetString(file.BlobPart.Data.GetBytes());
        if (!string.Equals(actualJson, expectedJson, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"O File '{FileName}' nao possui o conteudo esperado do teste B005. Nenhuma alteracao foi feita.");
        }
    }

    private static void ValidateDesignModel(KBModel designModel)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }
    }

    private sealed class ProbeObjects
    {
        public ProbeObjects(Procedure procedure, SDT sdt, Folder folder, WikiFileKBObject file)
        {
            Procedure = procedure;
            Sdt = sdt;
            Folder = folder;
            File = file;
        }

        public Procedure Procedure { get; }

        public SDT Sdt { get; }

        public Folder Folder { get; }

        public WikiFileKBObject File { get; }
    }

    private sealed class ProbeSnapshot
    {
        public ProbeSnapshot(int procedures, int sdts, int folders, int files)
        {
            Procedures = procedures;
            Sdts = sdts;
            Folders = folders;
            Files = files;
        }

        public int Procedures { get; }

        public int Sdts { get; }

        public int Folders { get; }

        public int Files { get; }

        public bool HasAny => Procedures > 0 || Sdts > 0 || Folders > 0 || Files > 0;

        public string DescribeCounts()
        {
            return $"Procedure={Procedures}, SDT={Sdts}, Folder={Folders}, File={Files}";
        }
    }
}
