# 09-INTEGRACAO_GeneXus_Extensibility_SDK.md

## Integração com GeneXus Extensibility SDK para o MVP

**Projeto:** Genexus Open API Builder
**Versão:** v1.0
**Base Primária:** 04-REQUISITOS_MVP_Genexus_Open_API_Builder.md v1.1
**Dependência direta:** 05-ARQUITETURA_FUNCIONAL_MVP.md v1.1
**Relacionamento adicional:** 07-UX_WIZARD_INICIAL.md v1.0 / 08-MODELO_DADOS_E_METADATA.md v1.0
**Objetivo:** definir integração técnica realista com GeneXus 18 U14 ou posterior via Extensibility SDK, usando Upgrade 15 como ambiente inicial de validação, separando fatos confirmados, hipóteses validáveis e o caminho técnico oficial.
**Idioma:** Português BR
**Público principal:** Agentes de IA + mantenedores humanos
**Data:** Abril/2026
**Última revisão:** Agosto/2026

---

# 1. Objetivo do Documento

Este documento existe para:

- reduzir risco técnico do projeto
- evitar assumir capacidades inexistentes
- orientar spikes objetivos
- conectar UX + metadata + geração
- preparar primeira prova real de funcionamento

Este documento **não substitui teste prático**, **não garante APIs internas**, **não autoriza hacks**.

---

# 2. Taxonomia

| Código | Tipo | Significado |
|------|------|-------------|
| DP-F04 | Decisão oficial | Requisito aprovado |
| AF-F05 | Arquitetura funcional | Fluxo oficial |
| UX-F07 | UX oficial | Wizard e interação |
| MD-F08 | Modelo interno | Dados e estruturas |
| SDK-F09 | Integração SDK | Definição deste documento |
| FP-F09 | Fato público | Evidência pública razoável |
| HP-F09 | Hipótese | Precisa spike |
| NA-F09 | Não assumido | Fora do MVP inicial |

---

# 3. Fontes e Rastreabilidade

| Código | Fonte |
|------|-------|
| F01 | Documentação pública GeneXus Extensibility / Platform SDK |
| F02 | Samples públicos oficiais |
| F03 | Documentos internos 04-08 do projeto |

---

# 4. Estratégia Oficial

No MVP:

1. provar integração mínima cedo
2. usar APIs públicas ou reproduzíveis
3. preferir simplicidade operacional
4. não travar projeto por feature ideal
5. validar criação de API Objects como único caminho REST
6. validar criação e persistência de Procedures, SDTs, Folder e File
7. tratar YAML nativo como saída de validação/regressão, não como fonte primária da geração

A partir de GeneXus 18 U14, a preparação `B010` usa o feed NuGet e os MSBuild SDKs oficiais (`GeneXus.Package.UI.Sdk` e dependências) em vez do instalador legado. O `B000` posterior comprovou no U15 o manifesto, o ponto de entrada e o mecanismo de instalação local; o limite inferior U14 foi confirmado em 2026-08-12 por usuário externo na Alpha (carregamento + geração). A configuração versionada não pode depender de caminhos da instalação do GeneXus.

[SDK-F09]

### 4.1 Evidência de B010 e limites

- **Fato oficial:** U14+ usa feed NuGet e MSBuild SDKs; isso substitui o instalador legado como método de build.
- **Fato de build:** o projeto mínimo restaurou e compilou em `net471`, gerando um `.nupkg` sem depender de DLLs da instalação.
- **Decisão de produto:** o MVP mira U14+ para manter uma única cadeia moderna; U15 é o primeiro ambiente de validação disponível.
- **Evidência B000:** a DLL Release com manifesto e classe de entrada foi descoberta, registrada e carregada pelo U15 local; o limite inferior U14 foi confirmado depois (2026-08-12) por usuário externo na Alpha.
- **Evidência B004:** o ciclo de vida de um API Object oficial foi validado no U15, com criação, alteração, releitura após reinstalação e exclusão confirmada por GUID.
- **Evidência B006:** metadata JSON em File preservou GUID, nome, descrição, bytes UTF-8 e SHA-256 após fechar e reabrir a KB de teste; o objeto temporário foi excluído ao final.
- **Evidência B020:** a detecção manual da KB ativa foi consolidada no fluxo somente leitura do protótipo navegável, conforme `Docs/Implementation/B020-DETECCAO-KB-ATIVA-PROTOTIPO.md`; a próxima responsabilidade operacional vigente fica no checkpoint `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`.
- **Evidência B021:** a listagem manual de 10 Transactions da KB ativa foi consolidada no fluxo somente leitura do protótipo navegável, conforme `Docs/Implementation/B021-LISTAGEM-TRANSACTIONS-ELEGIVEIS-PROTOTIPO.md`; a próxima responsabilidade operacional vigente fica no checkpoint `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`.
- **Evidência B022:** a seleção nativa manual de uma Transaction e a leitura de seu módulo foram consolidadas no fluxo somente leitura do protótipo navegável, conforme `Docs/Implementation/B022-LEITURA-MODULO-TRANSACTION-PROTOTIPO.md`.
- **Evidência B023:** a detecção manual dos nomes planejados para a Transaction selecionada foi consolidada no fluxo somente leitura do protótipo navegável, conforme `Docs/Implementation/B023-DETECCAO-OBJETOS-EXISTENTES-PROTOTIPO.md`; a próxima responsabilidade operacional vigente fica no checkpoint `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`.
- **Evidência B024:** a verificação manual da propriedade `Business Component` da Transaction selecionada foi consolidada no fluxo somente leitura do protótipo navegável, conforme `Docs/Implementation/B024-VERIFICACAO-BUSINESS-COMPONENT-PROTOTIPO.md`; a próxima responsabilidade operacional vigente fica no checkpoint `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`.
- **Evidência B025:** a leitura manual da chave primária simples e composta completa da Transaction selecionada foi consolidada no fluxo somente leitura do protótipo navegável, conforme `Docs/Implementation/B025-LEITURA-CHAVE-PRIMARIA-PROTOTIPO.md`; a próxima responsabilidade operacional vigente fica no checkpoint `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`.
- **Evidência B030:** o Passo 1 do wizard foi consolidado no fluxo somente leitura do protótipo navegável, selecionando `Transaction` pelo menu principal via seletor nativo e pelo contexto, conforme `Docs/Implementation/B030-SELECAO-TRANSACTION-WIZARD.md`; a próxima responsabilidade operacional vigente fica no checkpoint `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`.
- **Evidência B031:** o Passo 2 do wizard foi consolidado no protótipo navegável não persistente, configurando serviços, campos e filtros essenciais em memória, conforme `Docs/Implementation/B031-CONFIGURAR-CONTRATO-WIZARD.md`; a próxima responsabilidade operacional vigente fica no checkpoint `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`.

---

# 5. Capacidades Confirmadas Publicamente

| Capacidade | Evidência | Grau |
|------|-----------|------|
| Criar extensão para IDE | Docs públicas | Alto |
| Menus / comandos gerais | Samples públicos | Alto |
| Janela / pane / UI básica | Docs + samples | Médio |
| Empacotamento extensão | Docs públicas | Alto |
| Carregamento extensão na IDE | Samples | Alto |

## Observação

Confirmado significa existência geral.
Não significa garantia do fluxo exato deste produto.

[FP-F09]

---

# 6. Hipóteses Críticas com Critério de Aprovação

| Código | Hipótese | Aprovado quando |
|------|----------|-----------------|
| S01 | Extensão carrega | extensão inicia sem erro na IDE |
| S02 | Menu/comando disponível | comando aparece em algum canal suportado |
| S03 | UI abre | janela abre sem travar IDE |
| S04 | Contexto atual detectável | retorna nome/tipo do objeto selecionado |
| S05 | Ler KB atual | lista ao menos 1 Transaction real |
| S06 | Criar SDT | SDT teste criado e salvo |
| S07 | Criar Procedure | Procedure teste criada e salva |
| S08 | Criar API Object | API Object utilizável criado |
| S09 | Criar Folder | Folder teste criado ou reencontrado |
| S10 | Criar File | File JSON criado, salvo e relido após reabrir KB |
| S11 | Persistir alterações | save/update simples concluído |

[HP-F09]

---

# 7. Não Assumido no MVP

Itens desejáveis, porém não pré-condições:

| Item | Status |
|------|--------|
| Context menu visualmente sofisticado | Opcional |
| Wizard nativo sofisticado | Opcional |
| Toolbar dedicada | Opcional |
| Atualização in-place avançada | Opcional |
| Theme visual avançado | Opcional |
| Zero cliques extras | Opcional |

## Regra

Se ausente, o MVP continua viável.

A sofisticação visual do menu contextual é opcional; a entrada pelo menu de contexto da Transaction continua parte prevista do MVP.

[NA-F09]

---

# 8. Fluxo Técnico Oficial (alinhado F05/F07/F08)

Comando IDE
→ Resolver contexto
→ Abrir Wizard (F07)
→ Ler metadata (F08)
→ Montar ApiPlan (F08)
→ Executar geração (F05)
→ Persistir objetos e metadata em File
→ Mostrar resultado

[AF-F05][UX-F07][MD-F08]

---

# 9. Entrada na IDE

## Nome desejado

Generate Open API

## Prioridade de canais

1. Context menu Transaction
2. Menu principal com seleção nativa filtrada para Transaction
3. Comando geral IDE

O SDK público já demonstrou diálogo de seleção por tipo e suporte a seleção múltipla. O MVP usa seleção única filtrada para Transaction; seleção múltipla fica para fase posterior.

## Regra

Sempre usar o melhor canal realmente suportado.

[SDK-F09]

---

# 10. UI Oficial do MVP

A UI operacional deve seguir documento 07:

- wizard 3 passos
- navegação simples
- conflito no passo 2
- resumo final
- mensagem de sucesso/erro

## Se SDK limitar UI

Usar janela modal simples com steps internos.

[UX-F07][SDK-F09]

---

# 11. Dados Operacionais

Os dados lidos e produzidos devem seguir documento 08:

- TransactionInfo
- AttributeInfo
- SdtInfo
- ApiPlan
- ExecutionResult

[MD-F08]

---

# 12. Artefato REST Alvo

## Alvo único

O único artefato REST aceito é API Object oficial.

Procedure REST como artefato REST público, objetos REST equivalentes ou qualquer outro tipo de artefato REST estão fora do escopo do projeto. Procedures internas de apoio ao API Object fazem parte do MVP quando suportadas pelo SDK.

## Regra

Sem API Object viável, o MVP perde seu sentido atual.

[DP-F04][SDK-F09]

---

# 13. Sem Fallback

O projeto não adota fallback para Procedure REST como superfície pública, objetos REST equivalentes ou qualquer outro tipo que não seja API Object oficial.

Se a criação de API Objects via Extensibility SDK não for tecnicamente viável, o projeto perde seu sentido atual e deve ser reavaliado.

[SDK-F09]

---

# 14. Persistência / Save

## Necessário validar

- criar objeto novo
- salvar sem erro
- update simples
- refresh explorer

## MVP conservador

Se update for arriscado:

- bloquear a atualização antes de gravar
- manter existente intacto
- informar a divergência para decisão manual

O MVP não cria novo objeto versionado nem sufixos automáticos para resolver colisão.

[HP-F09][SDK-F09]

---

# 15. Política Anti-Hack

## Proibido

- scraping UI
- automação por clique
- reflection em internals privados
- editar arquivos ocultos manualmente
- dependência de IDs secretos

## Permitido

- APIs públicas
- samples oficiais reproduzíveis
- workaround documentado e estável

[SDK-F09]

---

# 16. Riscos de Compatibilidade

| Risco | Impacto |
|------|---------|
| Mudança assemblies GX18 updates | Médio |
| Namespace variar | Médio |
| Samples antigos | Médio |
| Context IDs limitados | Alto |
| Save API restrita | Alto |
| Tipo REST ideal indisponível | Médio |

## 16.1 Inventário dos instaladores GeneXus 18

Foi extraída a versão do `GeneXus.exe` dos instaladores GeneXus 18 disponíveis localmente usando SFX 7-Zip, sem executar o setup e sem instalar ou alterar qualquer versão do GeneXus. A tabela é um inventário de versões para orientar futuras portas da extensão; não é, sozinha, evidência de compatibilidade do produto ou do Extensibility SDK.

| Upgrade | FileVersion | ProductVersion |
|---------|-------------|----------------|
| U0 | `18.0.0.35399` | `18.0.0.166471` |
| U1 | `18.0.1.36838` | `18.0.1.167910` |
| U2 | `18.0.2.38467` | `18.0.2.169539` |
| U3 | `18.0.3.41029` | `18.0.3.172101` |
| U4 | `18.0.4.42578` | `18.0.4.173650` |
| U5 | `18.0.5.44509` | `18.0.5.175581` |
| U6 | `18.0.6.46862` | `18.0.6.177934` |
| U7 | `18.0.7.48055` | `18.0.7.179127` |
| U8 | `18.0.8.49527` | `18.0.8.180599` |
| U9 | `18.0.9.51026` | `18.0.9.182098` |
| U10 | `18.0.10.53188` | `18.0.10.184260` |
| U11 | `18.0.11.54344` | `18.0.11.185416+76329aa02b7f66b5b3684cf9f01395929b64f4f4` |
| U12 | `18.0.12.55001` | `18.0.12.186073+14950c6efae4e10975801ef72c767b5f5ba40f54` |
| U13 | `18.0.13.55604` | `18.0.13.186676+5dc04eebf2a605c230b805bf473f07c0a18b7444` |
| U14 | `18.0.14.56722` | `18.0.14.187794+1a9b6893388882269611c6e81ad6856708285e40` |
| U15 | `18.0.15.57673` | `18.0.15.188745+5b2216d446f916f7c0deae363fefd29ea087b8c3` |

O material coletado cobre U0 a U15, inclusive a U7. A U13 hotfix foi deliberadamente excluída desta matriz para manter o inventário no nível das versões principais de Upgrade. A confirmação de suporte deve continuar sendo registrada separadamente por build, carga da extensão, execução do Wizard e `Build All` em cada versão alvo.

## 16.2 Inventário dos instaladores GeneXus 17

A mesma extração SFX 7-Zip, somente `GeneXus.exe` e sem executar o setup, foi aplicada aos instaladores GeneXus 17 disponíveis localmente. A série oficial do GeneXus 17 vai de U0 a U11; a U11 foi o último upgrade antes do GeneXus 18.

| Upgrade | FileVersion | ProductVersion |
|---------|-------------|----------------|
| U0 | `17.0.0.14999` | `17.0.0.146071` |
| U1 | `17.0.1.16323` | `17.0.1.147395` |
| U2 | `17.0.2.17303` | `17.0.2.148375` |
| U3 | `17.0.3.18710` | `17.0.3.149782` |
| U4 | `17.0.4.20534` | `17.0.4.151606` |
| U5 | `17.0.5.21853` | `17.0.5.152925` |
| U6 | `17.0.6.23902` | `17.0.6.154974` |
| U7 | `17.0.7.25474` | `17.0.7.156546` |
| U8 | `17.0.8.26951` | `17.0.8.158023` |
| U9 | `17.0.9.28668` | `17.0.9.159740` |
| U10 | `17.0.10.30344` | `17.0.10.161416` |
| U11 | `17.0.11.32605` | `17.0.11.163677` |

O material coletado cobre U0 a U11, inclusive a U2. Esta tabela também não é, sozinha, evidência de compatibilidade do produto ou do Extensibility SDK.

[HP-F09]

---

# 17. Gates Técnicos Transversais do MVP

Os seguintes gates serão comprovados progressivamente nas Sprints 1–7, conforme cada capacidade for implementada. O conjunto deve estar aprovado antes do marco **wizard funcional do MVP concluído** e antes da Alpha:

1. extensão carrega no GeneXus 18 U14 ou posterior, com U15 como ambiente inicial
2. SDK cria, salva, reabre, altera e exclui objetos nativos `API`, `Procedure`, `SDT`, `Folder` e `File`
3. objeto `API` delega às Procedures e persiste `RestMethod`, `RestPath`, `Description` e `SecurityLevel`
4. YAML gerado pelo GeneXus reflete rotas, métodos, parâmetros, SDTs e nomes `_API_` (aprovado com ressalva das respostas HTTP declaradas 200/404 no YAML nativo)
5. `Create` e `Update` via BC funcionam com chave simples e composta, preservando regras e mensagens
6. filtro de `List` ausente é distinguido de vazio, `false` e zero, e campo obrigatório não preenchido é recusado com 400, sem membros públicos `Specified`
7. implementação controla códigos HTTP, corpo e `Location`, respeitando seu caráter opcional
8. `List` funciona com filtros opcionais, períodos, paginação, totalização e ordenação determinística
9. metadata em `File` sobrevive a fechar/reabrir a KB e reconhece objetos próprios
10. colisão, regeneração e remoção não sobrescrevem nem apagam objetos alheios

Se qualquer gate falhar sem alternativa nativa segura, o desenho deve ser revisto antes de declarar concluído o wizard funcional do MVP.

Não bloqueiam o MVP:

- associação visual sob a Transaction
- objeto `Documentation` como fonte de metadata
- uniformidade de erros interceptados antes da Procedure
- migração assistida após renomear ou mover Transaction
- GeneXus Next
- base compartilhada `api/v1`
- otimizações de build

[SDK-F09]

---

# 18. Critérios de Aceite da Integração MVP

| Critério | Resultado Esperado |
|------|--------------------|
| Existe entrada viável IDE | Sim |
| Existe UI mínima viável | Sim |
| Hipóteses possuem teste objetivo | Sim |
| API Object como único alvo definido | Sim |
| Docs 07 e 08 conectados | Sim |
| Anti-hack formalizado | Sim |

[SDK-F09]

---

# 19. Uso Correto por Agentes de IA

## Pode assumir

- há indícios razoáveis de suporte a comandos/menu
- UI simples é provável, porém validar
- API Object é o único alvo técnico aceito
- hipótese precisa prova prática

## Deve tratar com cautela

- nomes reais namespaces/classes
- APIs exatas da build instalada
- diferenças entre updates GX18

---

# 20. Conclusão Objetiva

O documento 09 transforma sonho em validação prática.

Primeiro provar integração mínima.
Depois automatizar geração REST com segurança.
