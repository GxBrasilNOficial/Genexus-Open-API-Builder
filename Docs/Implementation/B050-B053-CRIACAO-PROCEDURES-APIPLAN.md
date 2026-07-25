# B050-B053 — Criação real de Procedures a partir do ApiPlan

## Estado

B050-B053 foram validados manualmente no GeneXus 18 U15 como primeira escrita real de Procedures a partir do `ApiPlan` em memória e dos SDTs já existentes.

O comando adicionado é `Criar Procedures (B050-B053)`. Depois da validação inicial, a mesma etapa foi integrada ao encerramento de `Abrir Wizard (B030)`, executando somente após B040-B046 ser confirmado e concluído no mesmo fluxo do wizard.

## Escopo implementado

- registro do comando em `Package.cs`;
- registro do comando no manifesto `GenexusOpenApiBuilder.package`;
- criação de `ApiPlanProcedureWriter` para receber o `ApiPlan` em memória;
- confirmação modal obrigatória antes de qualquer escrita de Procedures na KB;
- reencontro obrigatório dos 7 SDTs produzidos por B040-B046 antes de criar Procedures;
- criação ou reencontro das 4 Procedures planejadas pelo `ApiPlan`;
- bloqueio conservador quando já existe Procedure com mesmo nome sem descrição sentinela do gerador;
- registro na Output de cada Procedure criada ou reencontrada.

## Comportamento de segurança

O comando não executa sem:

- KB ativa;
- `ApiPlan` em memória criado pelo wizard;
- `Transaction` em memória reencontrada na KB ativa;
- correspondência entre `ApiPlan.TransactionName` e a Transaction selecionada;
- SDTs próprios e compartilhados já existentes na KB ativa;
- confirmação explícita do usuário no modal da IDE.

Se qualquer condição falhar, a Output registra o bloqueio e nenhuma alteração é feita na KB.

## Objetos que podem ser escritos após confirmação

Procedures da Transaction selecionada:

- `proc<NomeBase>_API_List`;
- `proc<NomeBase>_API_Get`;
- `proc<NomeBase>_API_Create`;
- `proc<NomeBase>_API_Update`.

As Procedures são criadas no módulo da Transaction quando a API pública expõe esse módulo.

## Limites explícitos

B050-B053 não criam:

- API Object;
- comportamento REST completo;
- File de metadata persistente definitiva;
- permissões GAM;
- descrição `[Description]` em serviços reais;
- alteração dos SDTs já criados.

As Procedures criadas nesta frente contêm skeleton explícito de Sprint 5. A implementação REST real continua reservada à Sprint 6.

## Política de reencontro

O reencontro usa descrição sentinela do gerador:

```text
Genexus Open API Builder B050-B053 Procedure - <Backlog> - <Service>
```

Se já existir Procedure com mesmo nome e descrição diferente, a geração é bloqueada como colisão externa ou incompatível.

## Validação local

Validações executadas localmente:

```powershell
dotnet build Src\GenexusOpenApiBuilder.sln -c Release --no-restore
pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1
```

Resultado local: build Release com 0 erros e registro de 10 comandos sincronizado.

## Validação manual na IDE

A validação manual foi registrada em 2026-07-25 na Transaction `Contrato`.

Primeiro, a IDE confirmou o bloqueio seguro quando o comando foi executado sem `ApiPlan` em memória:

```text
[Genexus Open API Builder][B050-B053] Nenhum ApiPlan em memoria foi encontrado. Execute e conclua primeiro o comando Abrir Wizard (B030). Nenhuma alteracao foi feita na KB.
```

Depois de concluir `Abrir Wizard (B030)`, o comando reencontrou os 7 SDTs B040-B046 e criou as 4 Procedures planejadas:

```text
[Genexus Open API Builder][B050-B053] Escrita de Procedures concluida: Transaction='Contrato', PlannedProcedures=4, ReencounteredSdts=7, Created=4, Reencountered=0. Nenhum API Object, REST completo ou metadata persistente definitiva foi criado.
```

Procedures criadas:

- `procContrato_API_List`;
- `procContrato_API_Get`;
- `procContrato_API_Create`;
- `procContrato_API_Update`.

A evidência visual da IDE confirmou a presença das quatro Procedures no módulo da Transaction. A Output confirmou que nenhum `API Object`, REST completo ou metadata persistente definitiva foi criado.

## Integração com o wizard

Após a validação dos comandos separados, B050-B053 foi incorporado ao encerramento de `Abrir Wizard (B030)`. O wizard só oferece essa etapa quando a criação ou reencontro dos SDTs B040-B046 foi confirmada e concluída no mesmo fluxo. A Output deve registrar `Trigger='Wizard'` e preservar o limite de não criar API Object, REST completo ou metadata persistente definitiva.

Essa integração ainda precisa de validação manual no GeneXus 18 U15.

## Próximo passo

B050-B053 estão concluídos como comando separado. A próxima ação executável vigente é validar o fluxo integrado do wizard conforme o checkpoint `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`; B054 só deve ser promovido depois dessa evidência.
