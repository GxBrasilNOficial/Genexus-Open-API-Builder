#nullable enable

namespace GenexusOpenApiBuilder.Extension.Domain;

public static class ExtensionUiTerms
{
    public static string RoleLabel(ExtensionLanguage language, string role)
    {
        if (string.IsNullOrEmpty(role))
        {
            return string.Empty;
        }

        var gloss = language switch
        {
            ExtensionLanguage.PortugueseBrazil => PortugueseGloss(role),
            ExtensionLanguage.Spanish => SpanishGloss(role),
            _ => null,
        };

        return string.IsNullOrEmpty(gloss) ? role : role + " (" + gloss + ")";
    }

    public static string PortugueseChrome(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return string.Empty;
        }

        return source switch
        {
            "Security Level" => "Nível de segurança",
            "Default Page Size" => "Tamanho padrão da página",
            "Maximum Page Size" => "Tamanho máximo da página",
            "Default Page Size deve ser menor ou igual a Maximum Page Size." => "O tamanho padrão da página deve ser menor ou igual ao tamanho máximo da página.",
            "Defaults de geracao" => "Defaults de geração",
            "Servicos marcados por padrao" => "Serviços marcados por padrão",
            "Seguranca e paginacao" => "Segurança e paginação",
            "Preferencias gerais do wizard na KB ativa" => "Preferências gerais do wizard na KB ativa",
            "Marcar SDTs por padrao" => "Marcar SDTs por padrão",
            "Marcar Procedures por padrao" => "Marcar Procedures por padrão",
            "Marcar API Object por padrao" => "Marcar API Object por padrão",
            "Marcar metadata da API por padrao" => "Marcar metadata da API por padrão",
            "Marcar listagem por padrao" => "Marcar listagem por padrão",
            "Marcar REST via Business Component por padrao" => "Marcar REST via Business Component por padrão",
            "Marque ao menos um servico padrao." => "Marque ao menos um serviço padrão.",
            _ => source,
        };
    }

    private static string? PortugueseGloss(string role)
    {
        return role switch
        {
            "CreateRequest" => "criação",
            "UpdateRequest" => "atualização",
            "ListFilters" => "filtros",
            "Response" => "resposta",
            _ => null,
        };
    }

    private static string? SpanishGloss(string role)
    {
        return role switch
        {
            "CreateRequest" => "creación",
            "UpdateRequest" => "actualización",
            "ListFilters" => "filtros",
            "Response" => "respuesta",
            _ => null,
        };
    }
}
