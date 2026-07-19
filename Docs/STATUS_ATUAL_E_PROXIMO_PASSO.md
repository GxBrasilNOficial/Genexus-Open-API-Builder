# Status Atual e Próximo Passo

## Autoridade deste checkpoint

Este documento é a fonte canônica apenas para o estado operacional do projeto e para a próxima ação executável.

Ele não define requisitos funcionais nem contratos técnicos. Para essas decisões, prevalecem o [registro de decisões funcionais do MVP](Decisions/2026-07-14-REGISTRO_DECISOES_FUNCIONAIS_MVP.md) e os contratos ativos em [Foundation](Foundation/00-MASTER_INDEX_DO_PROJETO.md).

## Última atualização

2026-07-18.

## Último marco concluído

- entrevista funcional do MVP consolidada;
- documentos Foundation alinhados e auditados;
- `B010` revalidado pelo método oficial U14+: feed NuGet, MSBuild SDKs, lockfile, solution, projeto e pacote mínimos;
- `B011` concluído: estrutura interna confirmada sem introduzir projetos ou camadas vazias;
- `B012` concluído: convenções aplicáveis confirmadas sem antecipar objetos da geração.
- `B000` concluído no U15: extensão mínima compilada, registrada, marcada e carregada sem erro de compatibilidade com a DLL Release estável e os metadados públicos corrigidos; a coluna Description vazia foi aceita como limitação não bloqueante.
- `B001` concluído no U15: a extensão detectou a KB de teste `wsEducacaoSpTeste` pelo evento público `OnAfterOpenKB` e pela KB recebida em `e.KB`, exibindo nome, GUID e localização na janela Output sem operações de escrita.
- `B002` concluído no U15: a extensão listou 10 Transactions reais da KB de teste `wsEducacaoSpTeste` com `Transaction.GetAll(knowledgeBase.DesignModel)`, exibindo o total e os nomes na janela Output sem operações de escrita.
- `B003` concluído no U15: a extensão criou o Folder de teste `GxOpenApi_B003_Probe` no Root Module da KB `wsEducacaoSpTeste`, confirmado na janela Output e no painel Properties, sem alterar objetos preexistentes.
- correção de segurança pós-B003 validada no U15: a DLL atual foi reinstalada e a abertura de uma KB não emitiu mensagens B001–B003 na Output nem realizou escrita automática.
- `B004` concluído no U15: um API Object de teste foi criado, alterado, relido após reinstalação da extensão e excluído com ausência confirmada pelo GUID, exclusivamente por APIs públicas.
- `B005` concluído no U15: Procedure, SDT, Folder e File de teste foram criados, alterados, relidos e excluídos com ausência confirmada, exclusivamente por APIs públicas e com autorização explícita antes de cada fase de escrita.

## Frente ativa

**Sprint 1 — Spike de viabilidade**, correspondente ao pacote inicial da **Fase -1** do backlog.

## Próxima ação única

Iniciar `B006` — Validar persistência de metadata em File após fechar e reabrir a KB:

> Identificar e comprovar, pelas APIs oficiais disponíveis, que um File JSON de metadata criado pela extensão permanece íntegro e reencontrável após fechar e reabrir a KB de teste.

## Critério de conclusão e evidência esperada

- API pública de File reutilizada ou refinada para gravar metadata JSON;
- File de metadata criado, relido antes de fechar, relido após reabrir a KB e excluído ao final;
- log e instruções de reprodução registrados em documentação de implementação;
- nenhuma alteração em APIs ou objetos preexistentes;
- autorização explícita do usuário antes de cada fase de escrita na KB.

## Sequência operacional vigente

1. Sprint 0 executou a Fase 0 (`B010`–`B012`) e deixou a base de build reproduzível.
2. Sprint 1 executa o pacote inicial de viabilidade da Fase -1 (`B000`–`B006`).
3. As Fases 1–8 dependem da aprovação desse spike.
4. Sprint 2 entrega apenas o protótipo navegável e não persistente do wizard.
5. Sprint 3 cria metadata e `ApiPlan`.
6. Sprint 4 integra o wizard ao engine pela primeira vez e cria os SDTs.
7. Sprints 5–7 completam Procedures/API/metadata, serviços REST/segurança e o ciclo conservador de conflitos, regeneração e remoção.
8. O marco **wizard funcional do MVP concluído** ocorre ao final da Sprint 7, antes da Alpha.

## Bloqueios e fatos ainda não validados

- carregamento real do pacote em U14;
- compatibilidade prática das APIs do SDK com U14;
- comprovação progressiva dos gates técnicos transversais definidos nos documentos 09, 15 e 24.

A ausência do instalador Platform SDK não é bloqueio para U14+, porque a compilação usa o feed NuGet e os MSBuild SDKs oficiais. A proteção da instalação do GeneXus continua válida: o agente não escreve em `C:\Program Files (x86)\GeneXus`; o instalador controlado só copia a DLL quando o usuário o executa manualmente como administrador.

## Documentos governantes

- [06 — Backlog](Foundation/06-BACKLOG_v0.1.md)
- [09 — Integração com o SDK](Foundation/09-INTEGRACAO_GeneXus_Extensibility_SDK.md)
- [15 — Testes e qualidade](Foundation/15-TESTES_VALIDACAO_E_QUALIDADE.md)
- [24 — Plano por sprints](Foundation/24-PLANO_IMPLEMENTACAO_REAL_POR_SPRINTS.md)

## Marcos ainda não iniciados

- protótipo navegável do wizard;
- `ApiPlan`;
- engine de geração;
- Alpha público.

## Protocolo de atualização

Toda mudança de marco, frente ativa ou próxima ação deve atualizar este checkpoint no mesmo commit que produz a mudança. O checkpoint deve manter uma única próxima ação e apontar para os contratos, sem duplicá-los.

O fechamento de cada spike `B000`–`B006` também deve cumprir o checklist obrigatório de retirada de sondas temporárias, reinstalação da DLL passiva e alinhamento documental definido em `AGENTS.md`, antes de o marco ser considerado pronto para revisão pré-push.
