# Localização Trilíngue, Leitor de Filtros de APIs Existentes e Refinamentos de Contrato

## Contexto e Objetivo

Este documento registra a implementação de quatro frentes complementares:
1. **Localização trilíngue (pt-BR, es, en)** da extensão GeneXus Open API Builder;
2. **Leitura e restauração de contrato e filtros de APIs existentes** no Wizard (`PrototypeWizardExistingApiContractReader`);
3. **Diagnóstico de ownership e suporte a refresh intencional de contrato** (`DiagnoseOwnership` e `allowIntentionalContractRefresh`);
4. **Alinhamento do contrato HTTP de erro de `List`** com `Create`/`Update` e remoção do uso de `msg()`.

---

## 1. Localização Trilíngue da Extensão

### Componentes de Localização
- `Src/Domain/ExtensionLanguage.cs`: enum `ExtensionLanguage` (`PortugueseBrazil`, `Spanish`, `English`). A resolução usa o idioma da KB (`ReadLanguageValues(knowledgeBase)` / `Language.Get` em `ExtensionLocalization.Resolve`); sem KB aberta, o fallback é `English`. Não usa `CultureInfo.CurrentUICulture` nem o idioma da IDE.
- `Src/Domain/ExtensionUiTerms.cs`: rótulos de chrome em português (segurança, paginação, acentos das preferências) e `RoleLabel` para `CreateRequest`, `UpdateRequest`, `Response` e `ListFilters` no formato `Termo (gloss)` em pt-BR/es; em inglês o termo canônico permanece sozinho.
- `Src/Extension/ExtensionLocalization.cs`: catálogo central de strings com traduções completas para português, espanhol e inglês abrangendo:
  - Nomes de comandos de menu e categorias;
  - Títulos, rótulos, descrições e botões de todos os diálogos (`PrototypeWizardDialog`, `PrototypeWizardPreferencesDialog`, `ApiPlanTransactionSyncDialog`, `ApiPlanApplicationFinalReportDialog`, `PrototypeWizardContractDialog`, `PrototypeWizardReviewDialog`);
  - Mensagens de validação e motivos de bloqueio.
- `Src/Domain/ExtensionOutputLocalization.cs`: catálogo de mensagens formatadas para a janela Output da IDE GeneXus nos três idiomas suportados.

### Registro de Comandos no Manifesto
Para que os menus da IDE GeneXus exibam comandos localizados conforme o idioma resolvido da KB, `Src/Extension/GenexusOpenApiBuilder.package` e `Src/Extension/Package.cs` registram as definições de comando (`CommandDefinition`) correspondentes em conformidade com o checker `Tools/Test-ExtensionCommandRegistration.ps1`.

Esta frente alterou o manifesto (novos `CommandDefinition` em espanhol e inglês). A próxima instalação de teste na IDE exige `Register-ExtensionForGeneXus18.bat` e `genexus /install` no mesmo diretório da instalação; `Install-ExtensionForGeneXus18.bat` sozinho copia a DLL, mas não registra os comandos novos.

---

## 2. Leitura e Restauração de Filtros e Contrato de APIs Existentes

### Problema Resolvido
Ao reabrir o Wizard para uma Transaction que já possui API gerada, a interface anteriormente reiniciava a seleção de campos e filtros com os defaults de uma API nova, sobrescrevendo decisões deliberadas do usuário.

### Solução (`PrototypeWizardExistingApiContractReader.cs`)
- Inspeciona o `API Object` existente e o arquivo de metadata (`api<Transaction>_Metadata`);
- Restaura os serviços selecionados (`List`, `Get`, `Create`, `Update`);
- Restaura a seleção e ordem dos campos em `CreateRequest`, `UpdateRequest`, `Response` e `ListFilters`;
- Preserva a marcação de campos obrigatórios (`Required`) e parâmetros de paginação e ordenação;
- Se a metadata não estiver disponível, utiliza os SDTs próprios existentes como fallback.

---

## 3. Diagnóstico de Ownership e Refresh Intencional

- `ApiPlanApiObjectOwnership.cs`: adiciona método `DiagnoseOwnership` para auditoria e diagnóstico do estado de posse do API Object e objetos relacionados.
- `allowIntentionalContractRefresh`: o preflight de escrita (`ApiPlanWritePreflight.cs`) e os writers (`ApiPlanApiObjectWriter`, `ApiPlanBusinessComponentWriter`, `ApiPlanListProcedureWriter`, `ApiPlanMetadataFileWriter`) aceitam flag indicando alteração deliberada de contrato quando confirmada pelo usuário no Wizard ou Sync, permitindo atualizar o hash de integridade B067 sem falso bloqueio de colisão.

---

## 4. Alinhamento do Contrato HTTP de `List`

- A Procedure gerada para `List` (`ApiPlanListProcedureWriter.cs`) passa a usar a mesma estrutura de tratamento de erros de `Create` e `Update`:
  - Retorno de `ErrorResponse` (tipo `sdt_API_ErrorResponse`) e `RestStatusCode=400` em erros de validação de paginação e filtros;
  - Remoção de comandos `msg()` da lógica de geração;
  - Sources legados que continham `msg()` continuam sendo reconhecidos para migração conservadora sem perda de compatibilidade.

---

## Cobertura e Validação de Testes

- `Tests/Localization/Test-ExtensionLanguage.ps1`: valida a resolução determinística de idioma.
- `Tests/Localization/Test-ExtensionOutputLocalization.ps1`: valida a formatação de mensagens nos 3 idiomas.
- `Tests/WizardContract/Test-PrototypeWizardExistingApiFilters.ps1`: valida o contrato do reader de APIs existentes.
- Integrados ao gate pré-push `scripts/Invoke-PrePushMechanicalChecks.ps1` e verificados por `Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1`.

---

## 5. Checkpoint operacional 2026-08-15 (teste espanhol encerrado)

Anotação de sessão. O teste espanhol desta leva residual está encerrado. O teste inglês/italiano e o fingerprint B060 estão no §6.

### Recorte de produto (UI)

- Chrome de segurança/paginação só em pt/es, com acentos (`ExtensionUiTerms.PortugueseChrome`).
- Quatro papéis: `CreateRequest (criação|creación)`, `UpdateRequest (atualização|actualización)`, `Response (resposta|respuesta)`, `ListFilters (filtros)` em pt/es; inglês sem parêntese.
- Permanecem em inglês: `List`/`Get`/`Create`/`Update`, `Transaction`, SDT, Procedure, API Object, Wizard, Business Component, tokens persistidos `Authentication`/`Authorization`/`None`.
- Resolução de idioma = **Kb Language** (`ExtensionLanguageResolver` / `Language.Get`). Sem KB: `English`. Não usa `CurrentUICulture`. Enum: `PortugueseBrazil`, `Spanish`, `English`. Português de Portugal cai em inglês.

### Teste espanhol (encerrado)

KB: `wsEducacaoSpTeste`. Transaction principal: `NotaFiscal`. Também houve eliminação em `Teste` (Folder criado pela extensão).

Como ligar o idioma: Customization → Localization marcar o idioma **e** Properties **Kb Language** = o mesmo. Só habilitar não troca a UI da extensão.

O que passou em espanhol (prints + Output):

- Wizard (16 abas no código, nesta ordem): Serviços, Solicitudes, Response, Filtros de List, List, Rutas, Seguridad, Paginación, Ordenación, Obligatorios, SDTs, Procedures, API Object, Business Component, Metadatos, Resumen. Gloss `CreateRequest (creación)` etc. Motivos de bloqueio e operador `Contiene` traduzidos na UI.
- Resumen: parágrafo curto de Required em espanhol (havia chave só da variante longa).
- Informe Wizard: avisos B056 e Folder **completos** em espanhol no diálogo (antes a quebra a 96 colunas partia a frase). Botão `Abrir el objeto principal`.
- Output B054: leftover `a atualizacao do API Object` passou a `la actualización del API Object será absorbida...`.
- Sync sem diff: título `Ninguna sincronización necesaria.` (bug `Nenhum` prefixo de `Nenhuma` → `Ningúna sincronizacao necessaria.`).
- Sync com campo (`NotaFiscalObs6`): diálogo `Aplicar sincronización`, papéis com gloss, cancelamento sem gravar.
- Eliminar: `Sí`/`No` da extensão (não Sim/Não do Windows); pergunta `¿Confirma la eliminación?` visível; duas colunas (Procedures | SDTs); identificação numa linha; Folder/BC embaixo.

Aviso B056 *descripciones… fallback en inglés (idioma de la KB aún no validado por API pública)* é `PendingKbLanguageApiValidation`, **não** falha de catálogo de UI. Descrições de serviço no ApiPlan continuam em inglês até existir API pública de idioma da KB.

### Bugs encontrados e correções (DLL atual)

Arquivos centrais: `ExtensionOutputLocalization.cs`, `ExtensionLocalization.cs`, `ApiPlanApplicationFinalReport.cs` / `Dialog`, `Package.cs`, `ExtensionConfirmDialog.cs` (novo), testes em `Tests/Localization/`.

1. Replace curto `"Nenhum"` comia `"Nenhuma"` → headline `Ningúna…`. Frase completa da sync + `Nenhuma` antes de `Nenhum`.
2. Informe: `Translate` no corpo já quebrado a 96 colunas; avisos longos (Folder, B056) ficavam mistos. Traduzir **antes** de wrap.
3. Required curto do Resumen sem chave no `switch` ES/EN.
4. B054: após `tambem foi confirmado` restava `a atualizacao…` minúscula.
5. MessageBox Yes/No do SO em português. `ExtensionConfirmDialog` com Sí/No (es), Sim/Não (pt), Yes/No (en).
6. Layout do diálogo de eliminar: altura inflada, corte da última linha, MessageBox nativo substituído. Estado **aceito** pelo usuário: largura ~1040 px, duas colunas, fonte `SystemFonts.MessageBoxFont` (nesta máquina **Segoe UI 9 pt**, linha 16 px), empacote por `AutoSize` dos rótulos, janela um pouco acima do centro. Não reabrir layout salvo pedido novo.

Manifesto **não** mudou nestas correções residuais. Atualizar DLL: fechar IDE, `Install-ExtensionForGeneXus18.bat` como administrador. **Sem** `genexus /install` só por idioma ou por estas correções de chrome. `/install` continua necessário se, desde o último `/install` bem-sucedido, o `.package` / IDs de menu tiverem mudado (frente inicial de localização).

### O que ainda não foi testado (na data do §5)

- Na data deste checkpoint o teste **inglês** ainda não tinha começado. Encerrado no §6.
- pt-BR desta leva de correções residuais não foi revalidado na IDE após o diálogo largo (só es).

### Abas do Wizard (referência)

Ordem em `PrototypeWizardDialog.cs` (~152–167): Serviços, Requests, Response, Filtros List, List (geração), Paths, Segurança, Paginação, Ordenação, Obrigatórios, SDTs, Procedures, API Object, Business Component, Metadata, Resumo.

---

## 6. Inglês/italiano, diagnóstico B087 e fingerprint B060 (2026-08-15/16)

KB `wsEducacaoSpTeste`, Transaction `NotaFiscal`. Kb Language italiano resolve para `English` (não há `Italian` no enum). Manifesto inalterado; só `Install-ExtensionForGeneXus18.bat`.

### Localização

Chrome, informe, preferências, sync e Eliminar saíram em inglês. Leftovers corrigidos no catálogo/UI: `ausentes=`/`Dependencia`, motivo do UpdateRequest, `em memoria` (B031), `ApiPlan cobre`, prefixo `foi confirmado` (B054), `durante B071`, colisão (`Nenhuma escrita sera permitida` não pode passar por replace de `Nenhuma`). B056 *Service descriptions used an English fallback* permanece esperado (`PendingKbLanguageApiValidation`).

### Bloqueio falso do API Object

Após o primeiro apply italiano (13 updates, inclusive API + metadata), o Wizard seguinte bloqueava `apiNotaFiscal` com `GenerateApiObject=False` e `Cause='MetadataFingerprintMismatch'`. SDTs/Procedures reencontravam. O diagnóstico B087 na Output mostrou ownership, schema, GUIDs e **todo o baseline B067** compatíveis; só o SHA-256 do JSON compacto divergia (`FingerprintStored` ≠ `FingerprintActual`).

Causa: a gravação hasheia `generatedAtUtc` como string `DateTime.UtcNow.ToString("O")` (sete dígitos fracionários). A leitura usava `JObject.Parse`, que converte ISO-8601 em `DateTime`; o compacto do Newtonsoft corta zeros à direita. A conferência do fingerprint permanece; o parse da metadata passou a `ApiPlanMetadataIntegrity.ParseMetadataJson` / `ParseMetadataBytes` com `DateParseHandling.None`.

### Reteste U15 (2026-08-16)

`GenerateSdts/Procedures/ApiObject/Metadata/ApplyList/ApplyBusinessComponent=True`; preflight agregado aprovado; `Updated=13`, `Blocked=0`; `apiNotaFiscal` Guid `ee78dcc0-8dc6-480d-90e0-cf0ced1c83e9`; File `apiNotaFiscal_Metadata` `Status='Reencountered'`, `Bytes=78480`. Avisos restantes: fallback inglês das descrições e Folder `NotaFiscalOpenApi` reutilizado.


