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
            "Foi encontrada uma API gerada pela extensao sem o File de metadata '{0}'. A recuperacao criara somente esse File, com o inventario dos objetos encontrados na KB ({1} Procedures, {2} SDTs proprios, {3} compartilhados). Ela devolve a possibilidade de remover a API gerada, mas nao recupera o contrato original: paginacao, ordenacao, campos obrigatorios e a estrutura hierarquica nao existem fora da metadata perdida, e o Sincronizar seguira bloqueado ate uma nova aplicacao completa. Nada mais sera alterado. Deseja recuperar agora?" => "Foi encontrada uma API gerada pela extensão sem o File de metadata '{0}'. A recuperação criará somente esse File, com o inventário dos objetos encontrados na KB ({1} Procedures, {2} SDTs próprios, {3} compartilhados). Ela devolve a possibilidade de remover a API gerada, mas não recupera o contrato original: paginação, ordenação, campos obrigatórios e a estrutura hierárquica não existem fora da metadata perdida, e o Sincronizar seguirá bloqueado até uma nova aplicação completa. Nada mais será alterado. Deseja recuperar agora?",
            "A metadata '{0}' foi recuperada. Reabra o Wizard para continuar; nenhuma outra etapa foi executada nesta aplicacao." => "A metadata '{0}' foi recuperada. Reabra o Wizard para continuar; nenhuma outra etapa foi executada nesta aplicação.",
            "A recuperacao da metadata '{0}' falhou: {1}" => "A recuperação da metadata '{0}' falhou: {1}",
            "O File de metadata '{0}' existe, mas registra um API Object que nao esta mais na KB — tipicamente porque ele foi removido e outro foi gerado com o mesmo nome. Nesse estado o Wizard e o Remover ficam bloqueados. A recuperacao regravara esse File com o inventario atual ({1} Procedures, {2} SDTs proprios, {3} compartilhados) e o API Object que existe agora. O File ja era uma metadata reconstruida, sem o contrato original, entao nada de contrato se perde e o Sincronizar segue bloqueado ate uma nova aplicacao completa. Nenhum API Object, Procedure ou SDT sera alterado. Deseja recuperar agora?" => "O File de metadata '{0}' existe, mas registra um API Object que não está mais na KB — tipicamente porque ele foi removido e outro foi gerado com o mesmo nome. Nesse estado o Wizard e o Remover ficam bloqueados. A recuperação regravará esse File com o inventário atual ({1} Procedures, {2} SDTs próprios, {3} compartilhados) e o API Object que existe agora. O File já era uma metadata reconstruída, sem o contrato original, então nada de contrato se perde e o Sincronizar segue bloqueado até uma nova aplicação completa. Nenhum API Object, Procedure ou SDT será alterado. Deseja recuperar agora?",
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
