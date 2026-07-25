# B050-B053 — Criação real de Procedures a partir do ApiPlan

## Estado

B050-B053 foram preparados no runtime da extensão com um comando explícito de escrita na KB, ainda pendente de validação manual no GeneXus 18 U15.

O comando adicionado é `Criar Procedures (B050-B053)`.

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

## Validação pendente na IDE

A validação manual deve instalar a DLL atual e executar:

1. concluir `Abrir Wizard (B030)` para gerar o `ApiPlan` em memória;
2. garantir que B040-B046 já criou ou reencontrou os SDTs requeridos;
