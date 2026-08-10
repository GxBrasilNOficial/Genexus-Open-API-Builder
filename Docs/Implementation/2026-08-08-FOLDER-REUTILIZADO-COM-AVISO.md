# Folder reutilizado com aviso

## Objetivo

Alinhar o tratamento do Folder `<Transaction>OpenApi` preexistente no módulo correto à decisão funcional do MVP: reutilizar o contêiner com aviso explícito, sem tratá-lo como colisão e sem alterar conteúdo preexistente.

## Implementação

- `ApiPlanTransactionFolder` aceita um Folder de mesmo nome somente quando ele está no contêiner esperado da `Transaction` — `Parent` quando a `Transaction` está dentro de Folder, ou diretamente no módulo quando não está.
- Folder reutilizado não recebe `Save()`, não é realinhado e não tem a `Description` alterada. Description humana, vazia ou outro conteúdo preexistente é preservado.
- Sentinela de Description pertencente à extensão só é aceita quando corresponde exatamente à API atual; sentinela divergente, Folder em outro módulo/contêiner e mais de uma ocorrência continuam bloqueando.
- O Wizard e o relatório final propagam o aviso de reuso. A metadata mantém `transactionFolder.wasCreated=true` somente para Folder criado na execução e `false` para Folder reencontrado.
- A remoção continua usando `wasCreated`; Folder reutilizado nunca é apagado. Esse comportamento também foi validado na evidência de B086.
- A janela B081 passou a recalcular a necessidade de rolagem após o layout e ao redimensionar, evitando ocultar a última linha de avisos longos.

## Código, testes e build

- Código principal: `Src/Extension/Diagnostics/ApiPlanTransactionFolder.cs`, `ApiPlanGenerationStateReader.cs`, writers/preflight e `Package.cs`.
- Janela B081: `Src/Extension/ApiPlanApplicationFinalReportDialog.cs`.
- Testes: `Tests/TransactionFolder/Test-ApiPlanTransactionFolderReusePolicy.ps1` e `Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport.ps1`.
- `dotnet build Src\GenexusOpenApiBuilder.sln --configuration Release --no-restore`: aprovado, 0 avisos e 0 erros.
- Checker de pre-push de fixtures, teste de registro de comandos e `git diff --check`: aprovados.
- Manifesto/registro da extensão não foram alterados; somente a DLL exige instalação manual.

## Validação manual U15 — 2026-08-09

KB de teste, `Transaction='Teste'`, `Module='Root Module'`, Folder preexistente `TesteOpenApi`.

1. O Wizard entrou em estado `reencounter` e o resumo permitiu `Concluir e aplicar` sem bloqueios.
2. A aplicação reencontrou todos os artefatos: `Created=0`, `Updated=13`, `Blocked=0` e `Warnings=2`.
3. O Output B081 exibiu o aviso completo de reuso:

   `Folder preexistente 'TesteOpenApi' no contenedor correto sera reutilizado; a Description existente sera preservada e o Folder nunca sera removido pela remocao desta API.`

4. A janela do relatório final exibiu a barra de rolagem vertical; a última linha ficou acessível ao rolar até o fim. O texto completo também foi confirmado no Output.
5. O JSON exportado em `D:\Temp\apiTeste_Metadata.json` confirmou:

   ```json
   "transactionFolder": {
     "name": "TesteOpenApi",
     "wasCreated": false
   }
   ```

## Build All nos dois environments — 2026-08-09

Após a reexecução, o `Build All` foi executado nos dois environments da KB `wsEducacaoSpTeste`:

- `.NET Framework / SQL Server`: todas as etapas concluídas com sucesso, incluindo Specification, geração, SDTs, documentação REST, compilação e permissões GAM. O warning de cópia duplicada de `FBiTextSharp.dll` permaneceu ambiental e não bloqueante.
- `.NET / PostgreSQL`: todas as etapas concluídas com sucesso, incluindo Specification, geração, SDTs, documentação REST, Protocol Buffer, compilação e permissões GAM. O aviso de workloads do .NET foi informativo e não bloqueante.

Em ambos, `apiTeste`, as quatro Procedures e os sete SDTs foram especificados/gerados corretamente, e a documentação REST de `apiTeste` foi produzida.

## Remoção pelo menu — 2026-08-09

O comando **Remover API gerada** foi executado para a Transaction `Teste`.

- A confirmação exibiu `Folder: TesteOpenApi (reutilizado; nunca apagar)` e informou que o Business Component não seria revertido.
- O relatório final registrou `Outcome='Success'`, `Deleted=11`, `Blocked=0` e `Warnings=0`.
- Foram removidos somente o API Object, quatro Procedures, cinco SDTs próprios e o File de metadata.
- Os SDTs compartilhados, o Business Component e o Folder `TesteOpenApi` permaneceram na KB.

Status: **concluído**.

A comprovação integrada dos dez gates e o marco do wizard funcional do MVP foram concluídos em seguida (2026-08-09). A próxima ação vigente fica no checkpoint `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` (após `B088` concluído em 2026-08-10, a frente pré-Alpha restante é `B089`).

## Validação negativa U15 — 2026-08-09

Os cenários abaixo foram executados na KB de teste para comprovar o bloqueio conservador antes de qualquer escrita:

1. **Contêiner incorreto:** após remover a API, foi mantido somente `General\TesteOpenApi` para a `Transaction='Teste'` do `Root Module`. O Wizard exibiu um conflito com `Modulo='General'`, permaneceu bloqueado e o Output confirmou `GenerateSdts=False`, `GenerateProcedures=False`, `GenerateApiObject=False`, `GenerateMetadata=False` e `Nenhuma escrita foi solicitada`.
2. **Ambiguidade/duplicidade:** durante a preparação do cenário anterior, coexistiram `Root Module\TesteOpenApi` e `General\TesteOpenApi`. O Wizard exibiu dois conflitos e bloqueou sem escrita, comprovando a trava por mais de uma ocorrência do mesmo nome.
3. **Sentinela alheia:** com único Folder em `Root Module\TesteOpenApi` e Description `Genexus Open API Builder Transaction API folder - Transaction=Outra`, o Wizard exibiu um conflito, bloqueou e manteve todas as confirmações de escrita desmarcadas; a Description permaneceu intacta.
4. **Reuso com Description humana:** após a limpeza dos cenários negativos, foi recriado apenas o Folder `Root Module\TesteOpenApi` com Description `Folder mantido manualmente para teste U15`. O Wizard reutilizou o Folder com aviso, gerou a API com `Created=11`, `Updated=2`, `Blocked=0` e `Warnings=2`, e a metadata final confirmou `wasCreated=false`.

Os cenários negativos comprovaram módulo/contêiner incorreto, ocorrência ambígua e sentinela de outra Transaction; o caminho positivo comprovou preservação de Description humana, reuso e ausência de escrita parcial nos bloqueios.
