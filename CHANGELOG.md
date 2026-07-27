# CHANGELOG.md

# Changelog

Todas as mudanças relevantes deste projeto serão registradas neste arquivo.

O formato segue princípios de changelog legível e versionamento progressivo.

---

# [Unreleased]

## Added

- Estrutura inicial do repositório
- Pasta Docs organizada
- Foundation Docs 00 até 28
- checkpoint operacional `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`
- template de evidência reproduzível de `B010` em `Docs/Implementation`
- consolidação documental da entrevista funcional do MVP
- README inicial
- LICENSE MIT
- Planejamento da fase prática
- Sprint 0 concluído: build mínima reproduzível (`B010`–`B012`), solution e projeto de extensão em `Src`
- B000 concluído no U15: extensão mínima registrada, marcada e carregada na IDE com a DLL Release estável e os metadados públicos corrigidos
- B001 concluído no U15: detecção da KB ativa por API oficial, em modo somente leitura
- B002 concluído no U15: listagem de 10 Transactions reais por API oficial, em modo somente leitura
- B003 concluído no U15: criação controlada de Folder de teste com autorização explícita
- correção de segurança pós-B003 validada: a DLL atual não executa sondas automaticamente ao abrir uma KB
- B004 concluído no U15: ciclo de vida de API Object oficial comprovado com criação, alteração, releitura após reinstalação e exclusão confirmada
- B005 concluído no U15: ciclo de vida de Procedure, SDT, Folder e File comprovado com criação, alteração, releitura e exclusão confirmada
- B006 concluído no U15: metadata JSON em File preservou GUID, nome, descrição, bytes UTF-8 e SHA-256 após fechar e reabrir a KB
- B020 concluído no U15: detecção manual da KB ativa no fluxo do protótipo navegável, em modo somente leitura
- B021 concluído no U15: listagem manual de 10 Transactions da KB ativa no fluxo do protótipo navegável, em modo somente leitura
- B022 concluído no U15: seleção nativa manual de Transaction e leitura de seu módulo no fluxo do protótipo, em modo somente leitura
- B023 concluído no U15: detecção manual dos objetos planejados para a Transaction selecionada, incluindo `api<NomeBase>_Metadata`, em modo somente leitura
- B024 concluído no U15: verificação manual de `Business Component` da Transaction selecionada, em modo somente leitura
- B025 concluído no U15: leitura manual da chave primária simples e composta completa da Transaction selecionada, em modo somente leitura
- Menu principal `Genexus Open API Builder` validado no U15 com acesso aos comandos B020-B030, preservando o submenu de contexto da Transaction
- B030 validado no U15: `Abrir Wizard (B030)` seleciona `Transaction` pelo menu principal via seletor nativo e pelo contexto da Transaction, mantendo a escolha apenas em memória
- B031 validado no U15: `Configurar Contrato (B031)` navega sequencialmente por serviços, requests, response, filtros e resumo B032, acumulando escolhas apenas em memória sem criar `ApiPlan` nem alterar a KB
- B032 validado no U15: `Revisar Paths e Segurança (B032)` funciona pelo menu de contexto da `Transaction`, chama B031 automaticamente quando necessário e revisa paths, segurança, paginação e ordenação apenas em memória
- B033 validado no U15: `Abrir Wizard (B030)` passa a abrir o wizard único, absorvendo B031, B032 e B033 como páginas sequenciais e validando campos obrigatórios apenas em memória
- B034 validado no U15: cancelamento seguro do wizard único descarta `Transaction`, contrato, paths/segurança e obrigatoriedade em memória sem criar `ApiPlan` nem alterar a KB
- B035 validado no U15: wizard único verifica `Business Component`, bloqueia avanço sem BC e habilita a propriedade somente com confirmação explícita, mantendo decisões apenas em memória e sem criar `ApiPlan`
- B036 validado no U15: wizard único exibe campos tecnicamente inadequados desabilitados com motivo em `Requests` e `Filtros List`, mantém bloqueados não selecionáveis e registra contagens apenas em memória sem criar `ApiPlan`
- B037 validado no U15: wizard único consolida `Obrigatório no payload` para `CreateRequest` e `UpdateRequest`, explicita `Required` como presença do membro JSON e mantém decisões apenas em memória
- B038 validado no U15: wizard único monta `ApiPlan` inicial em memória para `Contrato`, com `MetadataFile='apiContrato_Metadata'`, 4 endpoints, 4 Procedures planejadas e 2 SDTs compartilhados, marcado como `IsEngineReady=false` e com pendências `UNRESOLVED_B038_*`, sem persistir metadata nem gerar objetos na KB
- Representação provisória de B090/B091 e B092 validada no U15 dentro do `ApiPlan`: wizard único classifica sensíveis e auditoria por política inicial hardcoded em memória, registra origem/razão no `ApiPlan` e grava `SecurityLevel` com `GamCondition='UNRESOLVED_B092_GAM_CONDITION'`, mantendo B090/B091 canônicos abertos até configuração por KB/metadata e sem persistir metadata nem gerar objetos na KB
- Follow-up da Sprint 3 validado manualmente: `ApiPlan` em memória resolve `GeneratorTarget='.NET'`, `ConflictMode='BlockOnCollision'` e `ReexecutionMode='Safe'`, mantendo naquele momento condição GAM e prontidão da engine explicitamente pendentes, sem persistir metadata nem gerar objetos na KB
- Contrato preparatório de configuração por KB para B090/B091 validado no U15 dentro do `ApiPlan`, ainda sem metadata persistente e sem gerar objetos na KB
- B056 validado no U15 dentro do `ApiPlan`: descrições de serviço resolvidas em memória com `ServiceDescriptionsPending=0/4`, idioma `English` e fallback técnico registrado, ainda sem aplicar `[Description]` em objeto `API` real naquele recorte preparatório
- B092 validado no U15 dentro do `ApiPlan`: `Authentication`, `Authorization` e `None` agora resolvem `GamCondition` explicitamente no plano, com confirmação obrigatória para `Authorization` e `None`, ainda sem aplicar segurança em objeto `API` real e sem gerar objetos na KB
- B039 validado no U15: preview de engine SDT recebe o `ApiPlan`, resolve cinco SDTs próprios e dois compartilhados com status `ResolvedSdtContractPreviewNoKbWrite`, registra `WritesKnowledgeBase=False` e não escreve na KB
- B040-B046 validados no U15: comando `Criar SDTs (B040-B046)` criou 7 SDTs a partir do `ApiPlan` após confirmação modal explícita na IDE, com 5 próprios e 2 compartilhados, sem Procedures, API Object ou metadata persistente definitiva
- B050-B053 validados no U15: comando `Criar Procedures (B050-B053)` bloqueou execução sem `ApiPlan`, depois criou 4 Procedures skeleton a partir do `ApiPlan` e reencontrou 7 SDTs B040-B046, sem API Object, REST completo ou metadata persistente definitiva
- `Abrir Wizard (B030)` validado no U15 oferecendo B040-B046 e B050-B053 em abas próprias de confirmação no wizard, com Output marcada com `Trigger='Wizard'`, reencontro de 7 SDTs e 4 Procedures existentes e preservação dos comandos separados para reexecução/diagnóstico
- Correção pós-revisão B040-B046/B050-B053 validada no U15: abas `SDTs` e `Procedures` do wizard confirmam a escrita antes do resumo, Output registra `GenerateSdts=True`, `GenerateProcedures=True`, `Trigger='Wizard'`, reencontro de 7 SDTs e 4 Procedures, sem modais pós-wizard e sem criar API Object, REST completo ou metadata persistente definitiva
- B054 validado no U15: `apiCarga` exibe `List`, `Get`, `Create` e `Update` na aba `Service Source`, cada serviço delegando sem parâmetros à Procedure skeleton B050-B053 correspondente; reexecução bloqueia fonte divergente
- wizard passou a reconhecer em leitura o estado atual de SDTs, Procedures e API Object, apresentando criação, complementação, reencontro ou bloqueio antes de qualquer escrita e usando `Concluir Teste` quando não há etapa confirmada
- Folder `<Transaction>OpenApi` criado ou reencontrado como irmão físico da Transaction, com realinhamento conservador em reexecuções
- B055 validado no U15: Create e Update passaram a ser aplicados via Business Component nas Procedures já geradas, com variáveis reais, Source/Rules persistidos nas partes públicas corretas, preflight conservador, validação em chave simples (`Carga`) e composta (`Teste`) e API Object sincronizado com assinatura/variáveis compatíveis
- B056 aplicado e validado no U15 em API Object real: `apiGuiaPed` recebeu `[Description]` nos serviços `List`, `Get`, `Create` e `Update`, preservou as assinaturas parametrizadas de B055 em `Create`/`Update`, e `Build All` passou gerando documentação REST
- B060 concluído no U15: wizard grava ou reencontra File JSON de metadata, persiste `External File Name` via `BlobPart.FileName`, bloqueia JSON inválido, identidade incompatível e colisão externa antes da escrita, preserva descrições especiais B056 com aspas, barra invertida e caracteres incomuns, exporta JSON válido e mantém o escopo sem completar REST, códigos HTTP finais ou segurança definitiva

## Fixed

- checkpoint preserva `B011` e `B012` antes de promover `B000`
- linha de corte do MVP passa a cobrir exaustivamente os itens necessários aos dez gates
- Sprints 3–7 distinguem ApiPlan, SDTs, Procedures/API/metadata, REST/segurança e operação conservadora
- referências de backlog, versões documentais e conflitos no wizard foram alinhadas
- layout inicial de `Src`, destino das evidências e ambiente-base de `B010` foram explicitados
- `Docs/Temp` foi protegido contra inclusão acidental no repositório público
- comandos experimentais B004 removidos do runtime após a validação do ciclo de vida do API Object
- comandos experimentais B005 removidos do runtime após a validação; o popup `Genexus Open API Builder` permanece no menu de contexto com o placeholder não operacional `Futura Primeira Opção`
- comandos experimentais B006 removidos do runtime após a validação de persistência; a sonda permanece apenas como evidência histórica não invocada
- B031 limpa contrato em memória ao trocar a Transaction no B030 ou ao detectar ausência de seleção válida, evitando reutilização de decisões antigas por passos posteriores
- B031 desabilita partes da chave primária no `UpdateRequest`, preservando a regra de chave completa no `RestPath`
- B031 desabilita fórmulas em requests por API pública e bloqueia chave primária no `CreateRequest` até validação pública de autonumeração, sem reflexão em internos do SDK
- B032 sincroniza `Services base path` com `ApiName` até edição manual e o consumo por B033 foi validado posteriormente dentro do wizard único
- B090/B091 alinhados como representação provisória hardcoded em memória; os itens canônicos permanecem abertos até configuração explícita por KB em metadata persistente
- B040-B046 e B050-B053 agora executam preflight completo antes de qualquer `Save()`: SDTs validam colisões, descrições sentinela e tipos; Procedures validam todos os SDTs próprios/compatíveis e todas as Procedures planejadas antes de gravar a primeira
- Texto do wizard alinhado ao contrato provisório: preflight B040-B046/B050-B053 não promete validação de escopo físico enquanto ownership depender de descrição sentinela e metadata persistente ainda estiver pendente
- SDT writer ampliado para tipos públicos usados na validação composta (BITMAP, BINARY, BINARYFILE, VIDEO, AUDIO, GEOGRAPHY, GEOPOINT, GEOPOLYGON e GEOLINE)
- Correção pós-revisão B055 sincroniza API Object e Procedures: o Service Source parametrizado de Create/Update agora é acompanhado das variáveis reais do API Object, gravadas pela coleção pública de variáveis, com preflight do contrato antes das gravações
- Correção pós-revisão B055 preserva domínios nos SDTs próprios de request/response usando membros baseados nos atributos da Transaction; rerun em `GuiaPed` reconfigurou SDTs próprios e `Build All` passou com `apiGuiaPed`, `procGuiaPed_API_Create` e `procGuiaPed_API_Update` no U15
- Correção pré-push B055 força reconfiguração dos SDTs requeridos mesmo quando somente Business Component é aplicado, adia o realinhamento do Folder até depois do preflight principal e bloqueia Procedures B055 e API Object B055 reencontrados com variáveis extras, ausentes ou com tipo, atributo base, domínio ou objeto nomeado incompatível; Procedure já B055 sem variáveis não padrão também deixa de ser reparada silenciosamente
- Correção B056 faz B054 atualizar API Object B055 legado para a variante B055 com descrições, sem remover parâmetros de `Create`/`Update`, e mantém B054 legado atualizado para B054 com descrições
- Correção B060 alinha o preflight visual do wizard ao writer real para validar também `ownership.transactionGuid` e `ownership.apiGuid` antes de apresentar o File de metadata como reencontro válido

## Planned

- Validar B061/B062: módulo/Folder dos objetos específicos da Transaction e nomenclatura padrão dos objetos persistidos

---

# [0.1.0] - 2026-04

## Added

- Criação oficial do projeto
- Definição de visão open source
- Coleção documental completa
- Estrutura base de diretórios
- Preparação documental para a futura fase de implementação

---

# Tipos de Mudança

- Added: nova funcionalidade
- Changed: alteração relevante
- Fixed: correção
- Removed: removido
- Deprecated: obsoleto
- Security: segurança

---

# Observação

Versões iniciais podem evoluir rapidamente durante a fase MVP.
