# B111 · Fase F1 — ordem de gravação e writer final único

**Sprint:** S-B111 — gravação única do API Object.
**Fase:** F1 de 3 (F1 ordem e writer final · F2 seam e receipts · F3 durabilidade e remoção).
**Data:** 2026-09-04. **Item:** `B111` em `Docs/Foundation/06-BACKLOG_v0.1.md`.

**Status:** plano para decisão humana. **Não** autoriza implementação, alteração de
código, instalação, commit ou push.

Este plano substitui, **para o escopo da F1**, o manuscrito v24
(`2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md`). O v24 continua sendo a referência das
fases F2 e F3, com as correções que as sondas impuseram e que estão registradas em
`2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md`.

---

## 1. Por que existe uma F1 separada

O v24 tratava numa única frente quatro coisas de tamanhos muito diferentes: a ordem de
gravação, um seam de persistência transversal, um diário durável com máquina de estados
de três dimensões, e a remoção de APIs legadas. Aplicadas juntas, o risco de introduzir a
degradação que a frente quer evitar fica maior que o risco atual.

A F1 isola o **benefício central de `B111`** — o API Object gravado uma única vez, pelo
consumidor final — e nada mais. Ela não depende da decisão entre modo A e modo B, que
passa a pesar apenas na F3.

As sondas de 2026-09-04 encolheram a F1 ainda mais do que o previsto: com identidade
estável disponível desde o `API.Create`, o aparato de marcador na `Description`, chave
alternativa e helper de identidade **deixa de ser necessário**.

---

## 2. Estado medido de partida

### 2.1 O que o código faz hoje

Confirmado por leitura em 2026-09-04:

1. `ApiPlanApiObjectWriter.CreateOrReencounter` prepara Folder, SDTs e Procedures e chama
   `Save()` tanto para API reencontrado (`ApiPlanApiObjectWriter.cs:707`) quanto para API
   criado (`ApiPlanApiObjectWriter.cs:718`), relendo por `API.Get(designModel, guid)` em
   seguida.
2. `ApiPlanBusinessComponentWriter` tem três `Save()` reais, e o do API está em
   `ApiPlanBusinessComponentWriter.cs:606`.
3. `ApiPlanListProcedureWriter` tem três `Save()` reais; o da Procedure em
   `ApiPlanListProcedureWriter.cs:934` e o do API em `ApiPlanListProcedureWriter.cs:955`.
3.1. Os dois writers montam uma lista `saveSteps` de pares `(Label, Action Save)` e a
   executam em laço com progresso e cronômetro — `ApiPlanBusinessComponentWriter.cs:98` e
   `ApiPlanListProcedureWriter.cs:66`. **Em ambos, o primeiro passo da lista é o API.**
   A inversão de ordem exigida por esta frente não está só em `Package.cs`: dentro de cada
   writer, o passo do API precisa passar de primeiro a último.
4. O Apply existe em **dois blocos** no mesmo `Package.cs`: o do Sync, em torno de
   `Package.cs:855`, e o do Wizard, em torno de `Package.cs:1600`.
5. Os dois blocos já contêm um deferimento **parcial** de B054, com a mesma regra:

   | Seleção | Comportamento atual | Consequência |
   |---|---|---|
   | `GenerateApiObject` sem BC | chama `TryCreateApiObject` | B054 salva o API antes de List, se houver |
   | `GenerateApiObject` + BC, API já existe por nome | não chama; BC absorve | um único Save, no BC |
   | `GenerateApiObject` + BC, API ausente | **chama** `TryCreateApiObject` | B054 cria e salva **antes** do BC |

6. `ApplyList` **não participa** dessa decisão. Em List-only, B054 salva o API antes do
   List — que depois salva de novo.
7. A existência do API é decidida por comparação de **nome**
   (`API.GetAll(...).Any(api => string.Equals(api.Name, apiPlan.ApiName, ...))`), tanto
   no Sync quanto no Wizard.

O diagnóstico do v24 se confirma, e fica mais preciso: o deferimento não é inexistente —
é **incompleto**. Ele cobre BC com API preexistente e ignora List por inteiro.

### 2.2 O que as sondas mediram

Do registro `2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md`, seis execuções sobre duas
KBs:

- o `Guid` de um API Object existe desde o `API.Create`, sobrevive a atribuições de
  `Name`/`Description` e ao `Save()`, e o objeto é reencontrável por ele;
- a `Description` preserva 256 caracteres e trunca o excedente em silêncio (`B112`);
- `Save()` sem alteração pendente custa ~0 ms; com alteração, Folder, SDT, Procedure e API
  custam dezenas de milissegundos, inclusive na KB grande;
- os `Save()` reais do pipeline são 16 pontos de código, alguns em laço.

**Consequência direta para a F1:** a identidade planejada do API é o `Guid` **lido** do
`Create`. Não se atribui `Guid`, não se grava marcador na `Description`, não existe chave
alternativa. Essa é a maior simplificação em relação ao v24.

---

## 3. Escopo da F1

### 3.1 O que entra

1. o predicado que decide se a frente assume a orquestração;
2. um contexto transient de API, carregando o `Guid` lido do `Create`;
3. a ordem física de gravação, com um único `API.Save()` no writer final;
4. um gate de pré-escrita **reduzido**, definido em 4.5;
5. separação entre nome planejado e nome persistido no relatório;
6. sentinelas de contrato e testes de fluxo listados na seção 6.

### 3.2 O que **não** entra, e sai declarado

| Fora da F1 | Vai para |
|---|---|
| seam de persistência injetável, `PersistenceReceipt` | F2 |
| classes de falha C, D e E distinguíveis | F2 |
| diário B111 durável, `LegacyImported` | F3 |
| três dimensões de estado, `JournalDurabilityUnknown` | F3 |
| recuperação automática, inventário físico | F3 |
| remoção de API legado com diário importado | F3 |
| decisão entre modo A e modo B | F3 |
| habilitação de BC diferida no Wizard com Save recibado | F2 (depende de receipt) |
| expansão da UI do Sync para a matriz do Wizard | fora da sprint |

A F1 **não promete** atomicidade, rollback, recuperação de estado parcial nem detecção de
Save com resultado indeterminado. Uma interrupção no meio da F1 deixa objetos parciais,
como hoje. O que ela garante é que o API Object não será persistido antes dos seus
consumidores — que é a causa da degradação observada em campo.

---

## 4. Contrato-alvo

### 4.1 Predicado

```
b111ManagedApply =
    GenerateApiObject || GenerateMetadata || ApplyBusinessComponent || ApplyList
```

Quando falso, o Apply segue pelo contrato existente sem assumir a orquestração da frente.
`GenerateMetadata` entra porque a metadata exige um API reencontrável e precisa participar
da mesma intenção.

O predicado deve existir nos **dois blocos** de `Package.cs`, com a mesma definição.

### 4.2 Writer final por seleção

| Seleção | Preparação B054 | Writer final | `API.Save()` físicos |
|---|---|---|---|
| API-only | prepara e persiste | B054 | 1 |
| BC-only | prepara sem salvar | BC | 1 |
| List-only | prepara sem salvar | List | 1 |
| BC + List | prepara sem salvar | List (BC não salva o API) | 1 |

Vale para Sync e Wizard. Com `GenerateApiObject=false`, nenhum writer chama `API.Save()`,
e o API precisa existir e ser validado antes do pipeline.

### 4.3 Contexto transient

Quando BC ou List participar, `ApiPlanApiObjectWriter` deixa de persistir e passa a
entregar um contexto ao writer final. Nome sugerido: `ApiPlanTransientApiContext`.

Conteúdo mínimo:

- o objeto `API` em memória, já configurado (Name, Description, Parent, ServiceGroupSource);
- `PlannedApiGuid` — o `Guid` **lido** do objeto, nunca atribuído;
- `PlannedApiName`;
- `TransactionGuid` e nome da Transaction;
- hash do contrato planejado;
- flags da seleção;
- writer final esperado;
- se o API é novo ou reencontrado.

Regras:

1. B054 não chama `API.Save()` quando BC ou List participa;
2. B054 não cria um segundo API temporário na KB;
3. os writers de BC e List **não podem** validar o contexto chamando `API.GetAll`,
   `FindApi` ou equivalente para um API novo — o objeto transitório é a fonte autorizada
   até o único Save final;
4. para um API que já existe, a resolução é por identidade validada, não por nome.

### 4.4 Ordem física

Depois do gate:

1. Folder, se necessária;
2. SDTs selecionados;
3. Procedures-esqueleto e Procedures de consumidores selecionadas;
4. BC, quando selecionado — **sem** salvar o API;
5. List, quando selecionado;
6. o único `API.Save()`, no writer final da tabela 4.2;
7. metadata, quando selecionada.

Nenhum consumidor pode salvar o API antes de seu Source, Rules e Variables estarem
finalizados. A metadata nunca precede o API fisicamente confirmado. Etapa não selecionada
é omitida, e omiti-la não autoriza gravação oculta.

A habilitação de BC por `transaction.Save()` **não** entra na F1: no Sync ela permanece
bloqueio de preflight, como já é; no Wizard, o diferimento com recibo depende do seam e
vai para a F2.

### 4.5 Gate reduzido

O gate do v24 tinha dezesseis validações, várias dependentes de diário e receipts. A F1
mantém as que não dependem de F2/F3:

1. Transaction resolvida por identidade estável, não apenas por nome;
2. nome planejado e nome persistido tratados como campos distintos;
3. contrato final completo, com hash determinístico;
4. seleção e flags coerentes com a entrada;
5. dependências necessárias presentes conforme as flags (SDTs, Procedures, Folder);
6. colisões de nome e posse de objetos existentes;
7. writer final resolvido e disponível para a seleção;
8. ausência de API existente com identidade conflitante;
9. nenhum writer fará descoberta por nome para substituir a identidade planejada.

O gate é barreira de escrita: se falhar, nenhum objeto do pipeline pode ter sido gravado.
As validações que dependem de diário, checkpoint ou estado de durabilidade ficam fora, e
o plano da F3 as reintroduz.

### 4.6 Matriz de `GenerateApiObject`

| `GenerateApiObject` | BC/List/metadata | API existente e próprio | API ausente |
|---|---|---|---|
| true | nenhum | qualquer | permitido — B054 é writer final |
| true | BC ou List | qualquer | permitido — writer final cria e salva uma vez |
| true | metadata | qualquer | permitido se o contrato criar API; metadata vem depois |
| false | qualquer | obrigatório | **bloqueio antes do primeiro Save** |

“API próprio” significa posse validada por identidade e contrato, não nome igual. Havendo
mais de um candidato ou identidade não confirmável, o resultado é bloqueio — nunca escolha
arbitrária.

No Sync, apenas as linhas com `GenerateApiObject=true`, `GenerateSdts=true`,
`GenerateProcedures=true` e `GenerateMetadata=true` são alcançáveis, porque
`BuildSelection` monta um perfil fixo. Os testes das linhas `false` devem declarar-se
Wizard-only, e a documentação não deve sugerir que o Sync oferece toggles que sua UI não
tem.

### 4.7 Relatório

O relatório deve distinguir:

- `PlannedApiName` e `PersistedMainObjectName`;
- `PersistedMainObjectGuid`;
- writer final efetivo;
- número de chamadas físicas a `API.Save()`.

`TryResolveMainObjectFromKb` não pode reencontrar o resultado final apenas por
`collector.ApiName` num fluxo gerenciado pela frente: a resolução usa o `Guid` planejado,
confirmado por releitura. Busca por nome fica como diagnóstico secundário e precisa
reportar ambiguidade.

---

## 5. Mudanças previstas por arquivo

| Arquivo | Mudança |
|---|---|
| `Src/Extension/Package.cs` | predicado nos dois blocos; deferimento de B054 estendido a List e a BC com API ausente; ordem física; resolução final por identidade |
| `Diagnostics/ApiPlanApiObjectWriter.cs` | separar preparação de persistência; devolver contexto transient; manter o Save só no caminho API-only |
| `Diagnostics/ApiPlanBusinessComponentWriter.cs` | aceitar o contexto transient; mover o passo do API para o **fim** de `saveSteps`; não salvar o API quando List participa; salvar uma vez quando for o writer final |
| `Diagnostics/ApiPlanListProcedureWriter.cs` | aceitar o contexto transient; mover o passo do API para o **fim** de `saveSteps`; executar o único `API.Save()` quando participar |
| `Diagnostics/ApiPlanWritePreflight.cs` | gate reduzido da seção 4.5 |
| `Diagnostics/ApiPlanApplicationFinalReport.*` | campos planejado × persistido e contador de `API.Save()` |
| `Tests/` | sentinelas e fluxos da seção 6 |

Nenhum caminho antigo pode sobreviver chamando B054 com `API.Save()` antes de BC ou List
quando `ApplyBusinessComponent` ou `ApplyList` for verdadeiro.

---

## 6. Testes da F1

### 6.1 Sentinelas de contrato

1. o predicado existe e é idêntico nos dois blocos de `Package.cs`;
2. `ApiPlanApiObjectWriter` só chama `API.Save()` no caminho API-only;
3. `ApiPlanBusinessComponentWriter` não chama `API.Save()` quando List participa;
4. existe exatamente um ponto de `API.Save()` por fluxo, no writer previsto;
5. B054 oferece contexto transient quando BC ou List participa;
6. os writers não chamam `API.GetAll`/`FindApi` para validar um API novo vindo do contexto;
7. `PlannedApiName` e `PersistedMainObjectName` são campos distintos;
8. não existe resolução final por nome isolado;
9. o Sync declara o perfil fixo e não anuncia flags que a UI não oferece.

Sentinelas são necessárias e insuficientes: elas provam forma, não comportamento.

### 6.2 Fluxos executáveis

| Entrada | Seleção | Resultado exigido |
|---|---|---|
| Wizard | API-only | um `API.Save()`, em B054 |
| Wizard | BC-only | um `API.Save()`, no BC |
| Wizard | List-only | um `API.Save()`, no List |
| Wizard | BC + List | um `API.Save()`, no List |
| Sync | sem BC/List | um `API.Save()`, em B054 |
| Sync | com BC | um `API.Save()`, no BC |
| Sync | com List | um `API.Save()`, no List |
| Sync | com BC + List | um `API.Save()`, no List |
| Wizard | `GenerateApiObject=false`, API próprio | consumidores atualizados sem `API.Save()` |
| Wizard | `GenerateApiObject=false`, API ausente | bloqueio antes do primeiro Save |
| Wizard | SDT/Procedure `false` | nenhuma gravação da etapa desmarcada |
| Sync | BC sem habilitação na Transaction | bloqueio antes de qualquer Save |

A contagem de `API.Save()` é o critério central. Sem o seam da F2, ela é verificada por
instrumentação simples do caminho de gravação, e não por interceptação injetada — o que a
F1 declara como limitação, não como equivalente.

---

## 7. Validação na IDE

Reinstalar a DLL conforme a política do repositório e repetir a validação **depois** da
DLL que contém a alteração; evidência de DLL anterior não vale para o gerador novo.

Numa Transaction de teste com objetos suficientes para exercitar SDT, Procedure, API, BC
e List:

1. Sync sem BC/List, com BC, com List, com BC+List;
2. Sync com BC selecionado numa Transaction sem BC habilitado — deve bloquear antes de
   qualquer gravação;
3. Wizard em API-only, BC-only, List-only, BC+List;
4. Wizard com `GenerateApiObject=false`, com API próprio e com API ausente;
5. Wizard com SDT e Procedure desmarcados;
6. em todos: o API aparece uma vez, e o relatório traz `Guid` persistido e writer final.

Registrar, na mesma passagem, o tempo de Apply antes e depois da mudança na KB grande. A
F1 não é frente de desempenho, mas a ordem física muda e a medição é barata.

---

## 8. Critérios de aceite

1. Os quatro fluxos de API têm exatamente uma chamada física a `API.Save()`.
2. O Save ocorre no writer final da tabela 4.2, em Sync e em Wizard.
3. B054 não salva o API quando BC ou List participa, inclusive quando o API é novo.
4. O gate reduzido ocorre antes de qualquer gravação do pipeline.
5. A identidade planejada é o `Guid` lido do `Create`; nenhum código atribui `Guid` nem
   grava marcador de identidade na `Description`.
6. API existente é resolvido por identidade e contrato, não por nome isolado.
7. O relatório separa planejado de persistido e informa o writer final.
8. `GenerateApiObject=false` com API ausente bloqueia antes do primeiro Save.
9. Flags de SDT e Procedure em `false` não produzem gravação oculta.
10. Nenhuma regressão nos fluxos existentes de Sync, Wizard e relatório.

Falhando qualquer critério, a F1 não está pronta para aceite.

---

## 9. Riscos

| Risco | Mitigação prevista |
|---|---|
| o writer de List deriva parâmetros do contrato **persistido** do API; com o API ainda em memória, essa derivação precisa passar a usar o contexto transient | tratar como o ponto mais delicado da F1; cobrir com fluxo executável BC+List e List-only antes de tocar em qualquer outra coisa |
| dois blocos de Apply divergirem de novo | sentinela que compara o predicado e a ordem entre os dois blocos |
| o deferimento parcial existente mascarar regressão em BC com API preexistente | fluxo executável específico para “BC com API já existente”, comparado ao comportamento atual |
| ausência de seam tornar a contagem de Saves frágil | declarada como limitação; a F2 substitui a instrumentação por interceptação |

---

## 10. Fontes

- `Docs/Implementation/2026-09-04-PLANO-API-OBJECT-GRAVACAO-UNICA.md` (v24, referência de F2/F3)
- `Docs/Implementation/2026-09-04-B111-SONDAS-IDENTIDADE-E-DIARIO.md` (medições)
- `Docs/Implementation/2026-09-04-EVIDENCIA-IDE-DRIFT-API-OBJECT.md` (linha de base de campo)
- `Src/Extension/Package.cs`, blocos de Sync e de Wizard
- `Src/Extension/Diagnostics/ApiPlanApiObjectWriter.cs`
- `Src/Extension/Diagnostics/ApiPlanBusinessComponentWriter.cs`
- `Src/Extension/Diagnostics/ApiPlanListProcedureWriter.cs`
- `Src/Extension/Diagnostics/ApiPlanWritePreflight.cs`
- `Docs/Foundation/06-BACKLOG_v0.1.md` — `B111`, e os colaterais `B112`, `B113`, `B114`
