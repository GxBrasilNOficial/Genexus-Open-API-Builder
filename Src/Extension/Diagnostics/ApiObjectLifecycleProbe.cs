using System;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// Executa o ciclo B004 somente por comandos explícitos. As fases posteriores
/// reencontram somente um objeto com nome e descrição de marcador B004 conhecidos.
/// </summary>
internal static class ApiObjectLifecycleProbe
{
    private const string ProbeName = "apiGxOpenApiB004Probe";
    private const string InitialDescription = "Gx Open API Builder B004 Probe - criado";
    private const string UpdatedDescription = "Gx Open API Builder B004 Probe - alterado";

    public static string Preflight(KBModel designModel)
    {
        ValidateDesignModel(designModel);

        var exists = API.GetAll(designModel)
            .Any(api => string.Equals(api.Name, ProbeName, StringComparison.OrdinalIgnoreCase));

        return exists
            ? $"Pré-verificação: já existe um API Object chamado '{ProbeName}'. Nenhuma alteração foi feita."
            : $"Pré-verificação: o nome '{ProbeName}' está disponível. Nenhuma alteração foi feita.";
    }

    public static string Create(KBModel designModel)
    {
        ValidateDesignModel(designModel);

        var exists = API.GetAll(designModel)
            .Any(api => string.Equals(api.Name, ProbeName, StringComparison.OrdinalIgnoreCase));
        if (exists)
        {
            return $"Criação bloqueada: já existe um API Object chamado '{ProbeName}'. Nenhuma alteração foi feita.";
        }

        var probe = API.Create(designModel);
        probe.Name = ProbeName;
        probe.Description = InitialDescription;
        probe.Save();

        var persistedProbe = API.Get(designModel, probe.Guid);
        return $"API Object de teste criado e relido: Name='{persistedProbe.Name}', Guid='{persistedProbe.Guid}', Description='{persistedProbe.Description}'.";
    }

    public static string Update(KBModel designModel)
    {
        var probe = GetVerifiedProbe(designModel);
        probe.Description = UpdatedDescription;
        probe.Save();

        var persistedProbe = API.Get(designModel, probe.Guid);
        return $"API Object de teste alterado e relido: Name='{persistedProbe.Name}', Guid='{persistedProbe.Guid}', Description='{persistedProbe.Description}'.";
    }

    public static string Read(KBModel designModel)
    {
        var probe = GetVerifiedProbe(designModel);
        var persistedProbe = API.Get(designModel, probe.Guid);
        return $"API Object de teste relido: Name='{persistedProbe.Name}', Guid='{persistedProbe.Guid}', Description='{persistedProbe.Description}'.";
    }

    public static string Delete(KBModel designModel)
    {
        var probe = GetVerifiedProbe(designModel);
        var probeGuid = probe.Guid;
        probe.Delete();

        var stillExists = API.GetAll(designModel).Any(api => api.Guid == probeGuid);
        if (stillExists)
        {
            throw new InvalidOperationException($"A exclusão do API Object de teste '{ProbeName}' não foi confirmada.");
        }

        return $"API Object de teste excluído e ausência confirmada: Guid='{probeGuid}'.";
    }

    private static API GetVerifiedProbe(KBModel designModel)
    {
        ValidateDesignModel(designModel);

        var probes = API.GetAll(designModel)
            .Where(api => string.Equals(api.Name, ProbeName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (probes.Length != 1)
        {
            throw new InvalidOperationException(
                $"Era esperado exatamente um API Object de teste chamado '{ProbeName}', mas foram encontrados {probes.Length}. Nenhuma alteração foi feita.");
        }

        var probe = probes[0];
        var hasExpectedDescription =
            string.Equals(probe.Description, InitialDescription, StringComparison.Ordinal) ||
            string.Equals(probe.Description, UpdatedDescription, StringComparison.Ordinal);
        if (!hasExpectedDescription)
        {
            throw new InvalidOperationException(
                $"O API Object '{ProbeName}' não possui a descrição esperada do teste B004. Nenhuma alteração foi feita.");
        }

        return probe;
    }

    private static void ValidateDesignModel(KBModel designModel)
    {
        if (designModel is null)
        {
            throw new ArgumentNullException(nameof(designModel));
        }
    }
}
