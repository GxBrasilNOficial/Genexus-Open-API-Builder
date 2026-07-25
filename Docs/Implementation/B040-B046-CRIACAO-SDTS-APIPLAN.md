# B040-B046 — Criação real de SDTs a partir do ApiPlan

## Estado

B040-B046 foram preparados no runtime da extensão com um comando explícito de escrita na KB, ainda pendente de validação manual no GeneXus 18 U15.

O comando adicionado é `Criar SDTs (B040-B046)`.

## Escopo implementado

- registro do comando em `Package.cs`;
- registro do comando no manifesto `GenexusOpenApiBuilder.package`;
- criação de `ApiPlanSdtWriter` para receber o `ApiPlan` em memória validado em B039;
- confirmação modal obrigatória antes de qualquer escrita na KB;
- criação ou reencontro dos SDTs compartilhados e próprios planejados;
- bloqueio conservador quando já existe SDT com mesmo nome sem descrição sentinela do gerador;
- registro na Output de cada SDT criado ou reencontrado.

## Comportamento de segurança

O comando não executa sem:

- KB ativa;
- `ApiPlan` em memória criado pelo wizard;
- `Transaction` em memória reencontrada na KB ativa;
- correspondência entre `ApiPlan.TransactionName` e a Transaction selecionada;
- confirmação explícita do usuário no modal da IDE.

Se qualquer condição falhar, a Output registra o bloqueio e nenhuma alteração é feita na KB.

## Objetos que podem ser escritos após confirmação

SDTs compartilhados:

- `sdt_API_ErrorResponse`;
- `sdt_API_Pagination`.

SDTs próprios da Transaction selecionada:

- `sdt<NomeBase>_API_CreateRequest`;
- `sdt<NomeBase>_API_UpdateRequest`;
- `sdt<NomeBase>_API_Response`;
- `sdt<NomeBase>_API_ListFilters`;
- `sdt<NomeBase>_API_ListResponse`.

Também pode criar ou reencontrar o Folder `GxOpenAPI` para os SDTs compartilhados.

## Limites explícitos

B040-B046 não criam:

- Procedures;
- API Object;
- File de metadata persistente definitiva;
- permissões GAM;
- descrição `[Description]` em serviços reais.

A política de reencontro ainda usa descrição sentinela do gerador, não metadata persistente definitiva.

## Validação local

Validações executadas localmente:

```powershell
dotnet build Src\GenexusOpenApiBuilder.sln -c Release --no-restore
pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1
git diff --check
```

Resultado local: build Release com 0 erros, registro de 9 comandos sincronizado e whitespace limpo.

## Validação pendente na IDE

A validação manual deve instalar a DLL atual e executar:

1. concluir `Abrir Wizard (B030)` para gerar o `ApiPlan` em memória;
2. executar `Criar SDTs (B040-B046)`;
3. confirmar o modal de escrita na KB;
4. conferir na Output os SDTs criados ou reencontrados;
5. confirmar no GeneXus que apenas os SDTs e o Folder necessário foram criados, sem Procedures, API Object ou File de metadata persistente definitiva.

## Próximo passo

Validar manualmente B040-B046 no U15. Se a criação falhar por limitação de API pública para tipos SDT referenciados, ajustar o writer antes de considerar a primeira escrita real concluída.
