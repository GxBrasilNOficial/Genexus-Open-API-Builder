#nullable enable

using System;

namespace GenexusOpenApiBuilder.Extension.Diagnostics;

/// <summary>
/// S-B111 / F3 — vocabulário fechado de <c>composite.role</c> do diário durável (decisão 30).
///
/// O papel descreve o nome arquitetural do alvo dentro da API gerada, e a F2 o recebe para
/// localizar um objeto visto pelo API Object **depois da poda por papel** — a poda consulta
/// <c>CompositeIdentity.Role</c> com <c>StringComparison.Ordinal</c>. Produzir papeis fora
/// deste vocabulário significa inventar um contrato que a poda nunca vai consultar.
///
/// O vocabulário vale para **quem escreve**. A leitura (schema, validador e a poda) aceita e
/// retorna qualquer valor, porque diários legados gravaram papeis que esta classe nunca
/// reconheceu (ex.: <c>ListFilters</c> como papel de um SDT) e nenhum deles pode ser rejeitado
/// de volta para quem os produziu. A fronteira, então, é assimétrica: produção fecha o
/// vocabulário; leitura preserva o histórico.
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
    /// Papel do procedimento de serviço (Prc) a partir do nome gerado. O nome segue o padrão
    /// <c>proc{Transaction}_API_{Service}</c> (case-insensitive), então o papel é o sufixo após
    /// o último <c>_API_</c>. Um nome fora do padrão lança, porque o chamador produziria um
    /// papel que a poda nunca consulta.
    /// </summary>
    public static string ForProcedureName(string procedureName)
    {
        if (string.IsNullOrWhiteSpace(procedureName))
        {
            throw new ArgumentException("Nome de Procedure vazio não define papel de serviço.", nameof(procedureName));
        }

        var marker = procedureName.LastIndexOf("_API_", StringComparison.OrdinalIgnoreCase);
        if (marker < 0)
        {
            throw new ArgumentException(
                "Nome fora do padrão proc{Transaction}_API_{Service}: '" + procedureName + "'.",
                nameof(procedureName));
        }

        return ForService(procedureName.Substring(marker + "_API_".Length));
    }

    /// <summary>
    /// Papel canônico de um nome de serviço, ou exceção quando ele não pertence ao vocabulário.
    /// A comparação é case-insensitive; o retorno é a escrita canônica.
    /// </summary>
    public static string ForService(string service)
    {
        foreach (var candidate in ServiceRoles)
        {
            if (string.Equals(candidate, service, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        throw new ArgumentException(
            "Serviço fora do vocabulário do diário: '" + service + "'. Esperado um de " + string.Join(", ", ServiceRoles) + ".",
            nameof(service));
    }
}