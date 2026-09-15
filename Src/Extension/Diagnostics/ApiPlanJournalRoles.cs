#nullable enable

using System;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// S-B111 / F3 — vocabulário fechado de <c>composite.role</c> do diário durável (decisão 30).
///
/// O papel descreve o nome arquitetural do alvo dentro da API gerada, e a F2 o recebe para
/// localizar um objeto visto pelo API Object depois da poda por papel — a poda consulta
/// <c>CompositeIdentity.Role</c> com <c>StringComparison.Ordinal</c>. Produzir papeis fora
/// deste vocabulário significa inventar um contrato que a poda nunca vai consultar.
///
/// O vocabulário vale para quem escreve. A leitura (schema, validador e a poda) aceita e
/// retorna qualquer valor, porque diários legados gravaram papeis que esta classe nunca
/// reconheceu (ex.: <c>ListFilters</c> como papel de um SDT) e nenhum deles pode ser rejeitado
/// de volta para quem os produziu. A fronteira, então, é assimétrica: produção fecha o
/// vocabulário; leitura preserva o histórico.
///
/// No caminho de escrita (Apply/Remover), preferir <see cref="TryForProcedureName"/> ou
/// <see cref="RequireForProcedureName"/>: um nome fora do padrão é falha de invariante da
/// extensão (renomeação manual), não <c>ArgumentException</c> de API interna.
/// </summary>
internal static class ApiPlanJournalRoles
{
    public const string MainApi = "MainApi";

    public const string OwnSdt = "OwnSdt";

    /// <summary>
    /// Papel de SDT usado por mais de uma API. Nunca vira identidade composta no diário:
    /// SDT compartilhado tem <see cref="JournalIdentityKind.None"/> (decisão 24), então o
    /// papel existe no vocabulário apenas porque a poda por papel o consulta.
    /// </summary>
    public const string SharedSdt = "SharedSdt";

    public const string Metadata = "Metadata";

    public const string List = "List";

    public const string Get = "Get";

    public const string Create = "Create";

    public const string Update = "Update";

    public const string Delete = "Delete";

    private static readonly string[] ServiceRoles =
    {
        List,
        Get,
        Create,
        Update,
        Delete,
    };

    /// <summary>
    /// Tenta obter o papel canônico a partir do nome gerado
    /// <c>proc{Transaction}_API_{Service}</c> (case-insensitive). Não lança.
    /// </summary>
    public static bool TryForProcedureName(string? procedureName, out string role)
    {
        role = string.Empty;
        if (string.IsNullOrWhiteSpace(procedureName))
        {
            return false;
        }

        var name = procedureName!;
        var marker = name.LastIndexOf("_API_", StringComparison.OrdinalIgnoreCase);
        if (marker < 0)
        {
            return false;
        }

        return TryForService(name.Substring(marker + "_API_".Length), out role);
    }

    /// <summary>
    /// Tenta obter o papel canônico de um nome de serviço. Não lança.
    /// </summary>
    public static bool TryForService(string? service, out string role)
    {
        role = string.Empty;
        if (string.IsNullOrWhiteSpace(service))
        {
            return false;
        }

        foreach (var candidate in ServiceRoles)
        {
            if (string.Equals(candidate, service, StringComparison.OrdinalIgnoreCase))
            {
                role = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Papel exigido no caminho de escrita. Falha com <see cref="InvalidOperationException"/>
    /// de domínio — nunca <see cref="ArgumentException"/> — para o orquestrador tratar como
    /// falha de etapa, não como bug de chamada.
    /// </summary>
    public static string RequireForProcedureName(string procedureName, string writeContext)
    {
        if (TryForProcedureName(procedureName, out var role))
        {
            return role;
        }

        throw new InvalidOperationException(
            writeContext
            + ": Procedure '"
            + procedureName
            + "' fora do padrão proc{Transaction}_API_{Service}; o diário não inventa composite.role.");
    }

    /// <summary>
    /// Papel do procedimento a partir do nome gerado. Delega a
    /// <see cref="RequireForProcedureName"/> com contexto genérico.
    /// </summary>
    public static string ForProcedureName(string procedureName) =>
        RequireForProcedureName(procedureName, "ApiPlanJournalRoles");

    /// <summary>
    /// Papel canônico de um nome de serviço, ou exceção de domínio quando fora do vocabulário.
    /// </summary>
    public static string ForService(string service)
    {
        if (TryForService(service, out var role))
        {
            return role;
        }

        throw new InvalidOperationException(
            "Serviço fora do vocabulário do diário: '" + service + "'. Esperado um de "
            + string.Join(", ", ServiceRoles) + ".");
    }
}
