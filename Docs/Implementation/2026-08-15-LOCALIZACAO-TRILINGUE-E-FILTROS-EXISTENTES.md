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
- `Src/Domain/ExtensionLanguage.cs`: enum `ExtensionLanguage` (`Portuguese`, `Spanish`, `English`) com resolução de idioma a partir do `CultureInfo.CurrentUICulture` ou configuração da IDE GeneXus.
- `Src/Extension/ExtensionLocalization.cs`: catálogo central de strings com traduções completas para português, espanhol e inglês abrangendo:
  - Nomes de comandos de menu e categorias;
  - Títulos, rótulos, descrições e botões de todos os diálogos (`PrototypeWizardDialog`, `PrototypeWizardPreferencesDialog`, `ApiPlanTransactionSyncDialog`, `ApiPlanApplicationFinalReportDialog`, `PrototypeWizardContractDialog`, `PrototypeWizardReviewDialog`);
  - Mensagens de validação e motivos de bloqueio.
- `Src/Domain/ExtensionOutputLocalization.cs`: catálogo de mensagens formatadas para a janela Output da IDE GeneXus nos três idiomas suportados.

### Registro de Comandos no Manifesto
Para que os menus da IDE GeneXus exibam comandos localizados conforme a preferência de idioma da extensão, `Src/Extension/GenexusOpenApiBuilder.package` e `Src/Extension/Package.cs` registram as definições de comando (`CommandDefinition`) correspondentes em conformidade com o checker `Tools/Test-ExtensionCommandRegistration.ps1`.

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
