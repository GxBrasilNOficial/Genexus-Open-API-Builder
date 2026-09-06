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
            "Delete marcado exige Get, Create e Update nos servicos padrao. Marque os tres ou desmarque Delete." => "Delete marcado exige Get, Create e Update nos serviços padrão. Marque os três ou desmarque Delete.",
            "Diagnostico e recuperacao" => "Diagnóstico e recuperação",
            "Opcoes de investigacao. Nao sao necessarias no uso normal da extensao." => "Opções de investigação. Não são necessárias no uso normal da extensão.",
            "Oferecer recuperacao de metadata orfa no Wizard" => "Oferecer recuperação de metadata órfã no Wizard",
            "Suprimir a atualizacao da tela durante as gravacoes - a janela congela e Abortar nao responde (B109)" => "Suprimir a atualização da tela durante as gravações — a janela congela e Abortar não responde (B109)",
            "Foi encontrada uma API gerada pela extensao sem o File de metadata '{0}'. A recuperacao criara somente esse File, nao alterara API Object, Procedures ou SDTs, e encerrara esta aplicacao para uma nova leitura limpa. Deseja recuperar agora?" => "Foi encontrada uma API gerada pela extensão sem o File de metadata '{0}'. A recuperação criará somente esse File, não alterará API Object, Procedures ou SDTs, e encerrará esta aplicação para uma nova leitura limpa. Deseja recuperar agora?",
            "A metadata '{0}' foi recuperada. Reabra o Wizard para continuar; nenhuma outra etapa foi executada nesta aplicacao." => "A metadata '{0}' foi recuperada. Reabra o Wizard para continuar; nenhuma outra etapa foi executada nesta aplicação.",
            "A recuperacao da metadata '{0}' falhou: {1}" => "A recuperação da metadata '{0}' falhou: {1}",
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
