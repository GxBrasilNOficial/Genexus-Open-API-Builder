# B111 — sondas de identidade e de diário (evidência de IDE, 2026-09-04)

**Natureza:** evidência de runtime coletada durante o **planejamento** de `B111`, antes de
qualquer implementação. Não altera código de produção nem decide a frente; substitui
suposições do plano por medições.

**DLLs que produziram esta evidência** (`GenexusOpenApiBuilder.Extension.dll`, builds
Release locais, na ordem em que foram usadas):

| SHA-256 | O que acrescentou |
|---|---|
| `E0A7C07087D5576161C26A04F9355FB6E651EED7B1A60B66709B2A38C8AC48B6` | S1, S2, S3 (manifesto alterado; `genexus /install` executado) |
| `A2D565D493FA7AEC26D9812582D9E23E86A04D20AE51225BAF9FA40D2CB08B91` | S4 — custo de atualizar o diário |
| `1FF2029B6A5222E588209A35AE9340B5F007FD5F2808C923059C69020490CF3E` | S5 — custo de gravação por tipo |
| `E31FF953EE0CFE2E2B2C5B5D7B2AAC7387B6458687AD30B73F1CFA1F59E35DEE` | S1 sem atribuir Guid, S1b isolado, S2 cronometrado, SDT válido em S5 |

**Instrumento:** comando temporário **Sonda B111**, implementado em
`Src/Extension/Diagnostics/B111IdentityProbe.cs` e
`Src/Extension/Diagnostics/B111JournalProbe.cs`. Cria os próprios objetos de teste,
mede e os exclui; não toca em objeto pré-existente da KB nem participa de Apply, Sync
ou remoção. **É sonda temporária:** ao fechar a frente, remover o comando das três
camadas de registro e executar `Tools/Test-ExtensionCommandRegistration.ps1`.

**Alcance da evidência.** Vale para a DLL acima, para a versão de IDE usada e para a KB
medida. Qualquer alteração em writers, mapa de contrato ou identidade exige reinstalar a
DLL e repetir a medição; artefato já presente na KB não é medição do gerador vigente.

---

## 1. Por que a sonda foi necessária

A análise estática do SDK não respondeu às perguntas. Os corpos de método de
`Artech.Udm.Framework.Entity` na instalação vêm como stub — `Entity.get_Guid` tem IL
`00-14-A5-1F000001-2A` (`nop; ldnull; unbox.any; ret`) —, então não é possível ler por
metadata como o `Guid` é atribuído.

O que a análise estática **conseguiu** estabelecer, e continua valendo:

- a cadeia de herança é `API` → `KBObject` → `Artech.Udm.Framework.Entity`;
- `Entity.Guid` declara getter e **setter** públicos virtuais, e existe
  `Entity.SetUdmEntityGuid` (internal);
- não há chamada a `Guid.NewGuid` em `Artech.Udm.Framework.dll`,
  `Artech.Architecture.Common.dll` nem nas demais `Artech.Udm.*` — o GUID não nasce por
  essa via no cliente.

---

## 2. S1 — identidade do API Object antes do primeiro Save

| Momento | `Guid` observado |
|---|---|
| logo após `API.Create`, sem nenhuma atribuição | `32d0a01f-93f9-42be-b092-458eb726430f` |
| após atribuir `Name` e `Description`, ainda sem Save | inalterado |
| após atribuir um GUID escolhido (por reflexão) | aceito: `9e22f7ea-…03b0` |
| imediatamente antes do `Save()` | `9e22f7ea-…03b0` |
| imediatamente após o `Save()` | `9e22f7ea-…03b0` (inalterado) |
| `API.Get(designModel, guid)` pós-Save | reencontrou `apiGoabB111IdentityProbe` |

**Conclusão.** O API Object tem identidade estável **desde o `Create`**, antes de
qualquer gravação, e essa identidade sobrevive ao `Save()` sem ser substituída.

**Recomendação de desenho: ler, não atribuir.** A sonda demonstrou que o setter aceita
uma atribuição, mas isso é desnecessário — o GUID do `Create` já serve como identidade
planejada. Ler evita a reflexão, evita escolher um GUID que possa colidir e não depende
de o setter ser público em tempo de compilação (a sonda usou `Public | NonPublic`, então
esse ponto permanece não medido).

**Efeito sobre o plano.** O ramo alternativo de identidade (`NewApiIdentityKey`) e o
marcador `GOAB-B111-IDENTITY` gravado na `Description` deixam de ser necessários, junto
com o helper que os montaria, validaria e protegeria contra truncamento. A recuperação
de resultado indeterminado do Save passa a ser uma leitura por chave
(`API.Get(designModel, plannedGuid)`) seguida de conferência de contrato, em vez de
enumeração de candidatos por identidade.

---

## 3. S2 — capacidade da `Description` no round-trip

Gravação seguida de releitura, no mesmo objeto:

| Tamanho gravado | Relido | Resultado |
|---|---|---|
| 120 chars | 120 | preservado |
| 250 chars | 250 | preservado |
| 500 chars | 256 | truncado (prefixo comum 256) |
| 1.000 chars | 256 | truncado |
| 2.000 chars | 256 | truncado |
| 4.000 chars | 256 | truncado |
| descrição canônica + marcador real (209 chars) | 209 | preservado |

**Conclusão.** A `Description` preserva **256 caracteres** e trunca o excedente **em
silêncio**: o `Save()` não falha nem avisa.

Isto deixou de bloquear `B111` porque S1 tornou o marcador desnecessário. Permanece,
porém, como passivo latente independente desta frente, registrado como `B112`:
`ApiPlanOwnedObjectDescription` usa a Description canônica como prova de posse
histórica, e um nome longo o bastante a truncaria sem erro visível.

---

## 4. S3 e S4 — custo e viabilidade do diário durável

Medido em **seis execuções** sobre duas KBs: `wseducacaospteste` (KB pequena, 22 Files) e
`fabricabrasil18test` (cópia de teste da KB grande de produção, 93 Files).

### Varredura — escala com a KB, sempre

| Medição | KB pequena | KB grande |
|---|---|---|
| `WikiFileKBObject.GetAll` materializado | 4–6 ms | 19–27 ms |
| índice completo (7 varreduras) | 106–124 ms | **2.560–2.967 ms** |
| remonte do índice | 88–132 ms | **2.792–3.285 ms** |
| varredura por prefixo (recuperação) | 5–6 ms | 22–28 ms |
| releitura por nome via `GetAll` | 4–7 ms | 19–45 ms |
| releitura direta por `Id` | **0 ms** | **0 ms** |

### Gravação — o custo é do tipo `File`, não do tamanho da KB

Custo de `Save()` **com** alteração, por tipo de objeto, na mesma KB e na mesma execução:

| Tipo | KB pequena | KB grande |
|---|---|---|
| Folder | 11–14 ms | 12–33 ms |
| SDT | 16–21 ms | 16–34 ms |
| Procedure | 19–22 ms | 21–35 ms |
| API | 25–35 ms | 32–98 ms |
| **File (o diário)** | **116–160 ms** | **866–1.352 ms** |

Primeira gravação (criação do objeto):

| Tipo | KB pequena | KB grande |
|---|---|---|
| Folder | 12–17 ms | 11–48 ms |
| SDT | 35–79 ms | 41–226 ms |
| Procedure | 42–84 ms | 68–174 ms |
| API | 43–51 ms | 62–64 ms |
| **File** | **134–157 ms** | **1.027–1.293 ms** |

Em todos os tipos e nas duas KBs, `Save()` **sem alteração pendente** custou 0 a 3 ms:
não há taxa de commit, o preço é pago por gravação efetiva.

### Uma correção de percurso, registrada de propósito

Após a segunda execução na KB grande — a única em que o custo do File caiu para 21–147 ms
— este documento chegou a registrar que o custo de ~1,3 s seria anômalo e que a alavanca
de desempenho seria a varredura, não a gravação. **Essa correção estava errada.** Nas
quatro execuções seguintes o File voltou a custar de 0,87 s a 1,35 s na KB grande. Das
seis execuções, cinco mostram o custo alto; a exceção foi justamente a execução em que
S1 abortou antes de qualquer gravação de API, e continua sem explicação.

O que a série mostra, e a execução isolada escondia, é mais específico e mais útil: o
custo alto **não é do Save em geral nem do tamanho da KB** — Folder, SDT, Procedure e API
custam dezenas de milissegundos na KB grande, medidos segundos depois, na mesma sessão.
O custo alto é do tipo `WikiFileKBObject`, e é ele que escala mal: de ~130 ms na KB
pequena para ~1,1 s na grande, contra uma variação de poucos milissegundos nos demais
tipos.

S1 e S2 deram resultado idêntico em todas as seis execuções — GUID estável desde o
`Create`, `Description` truncada em 256.

Nome determinístico produzido pela fórmula da seção 4.6 do plano:
`B111_J_b7de9b06e117b361ff59c0df` (31 caracteres), idêntico nas duas KBs, como esperado
de uma fórmula determinística sobre entradas fixas.

### 4.1 O custo do diário é de escrita, e o diário é um File

A leitura é barata em qualquer caminho e em qualquer tamanho de KB: **0 ms** por `Id`,
0 ms pelo índice, 19 a 45 ms por varredura. **A gravação é o problema, e o problema é o
tipo escolhido para o diário.**

Cinco fatos que a série estabeleceu:

1. **Atualizar custa o mesmo que criar.** Na KB grande, criar o File custou 1,03–1,29 s e
   os updates custaram 0,87–1,35 s. A hipótese de que o update seria mais barato está
   refutada.
2. **O custo não depende do payload.** 247 bytes e 20 KB custaram o mesmo. Enxugar o
   conteúdo do diário não compra desempenho.
3. **O custo não depende do que mudou.** Alterar só a `Description`, sem tocar no Blob,
   custou o mesmo que reescrever o Blob.
4. **O custo é por gravação efetiva.** `Save()` sem alteração pendente custa 0 ms em
   todos os tipos e nas duas KBs.
5. **O custo é específico do `File`.** Na mesma KB grande e na mesma execução, Folder,
   SDT, Procedure e API gravam em dezenas de milissegundos. Só o File custa ~1,1 s.

Orçamento para o modo A na KB grande, a ~1,1 s por gravação de diário:

| Política de atualização do diário | Gravações | Custo acrescido ao Apply |
|---|---|---|
| uma por etapa confirmada, como no manuscrito expandido v24 | ~10 | **~11 s** |
| três checkpoints (início, pós-API, conclusão) | 4 | ~4,4 s |
| mínimo defensável (criação + conclusão) | 2 | ~2,2 s |

Sobre os ~49 s de Apply observados em campo, a política do manuscrito expandido acrescentaria da ordem de
20% do tempo total apenas em bookkeeping. **O plano precisa trocar “atualizar o diário
após cada Save confirmado” por checkpoints agrupados**, e declarar o orçamento medido.
A escolha entre modo A e modo B tem preço: o modo A custa entre 2,2 s e 11 s de Apply na
KB real, conforme a granularidade, mais um File por aplicação.

Há ainda uma consequência fora do diário: **a metadata B060 também é um `WikiFileKBObject`**.
Se o custo medido valer para ela, o pipeline já paga cerca de 1,1 s por gravação de
metadata na KB grande hoje, antes de qualquer mudança desta frente.

### 4.2 Nunca remontar o índice para enxergar o diário

O remonte do índice custa **3,155 s** na KB grande. Qualquer desenho que remonte o
índice para reencontrar o diário paga esse preço a cada vez, e por isso está descartado —
ver 4.3.

### 4.3 O índice pré-Apply não enxerga o diário

Mesma armadilha que já obrigou `RefreshFolders` e `RefreshSdts`: um índice montado antes
da gravação não contém o objeto novo. `ApiPlanKbObjectNameIndex` **não possui**
`RefreshFiles`.

A saída recomendada não é acrescentar esse refresh — que custaria cerca de 3,1 s por
chamada na KB grande —, e sim guardar a identidade do diário no momento da criação e
relê-lo diretamente por ela durante o fluxo, sem tocar no índice depois. O índice serve
para a resolução inicial ao abrir o fluxo, onde a busca pelo nome determinístico custa
0 ms.

**A identidade a guardar é o `Id`, não o `Guid`.** Diferente de `API`, que expõe
`API.Get(designModel, guid)`, `WikiFileKBObject.Get` só aceita `int` — não há overload
por `Guid`. A releitura por `Id` custou **0 ms** nas duas KBs, contra 7 a 45 ms da
varredura por nome. O plano não pode dizer “reler o diário por GUID”: é o `Id` que
precisa ser guardado, e o `Guid` permanece útil apenas como registro de identidade.

### 4.4 Correção de redação exigida no plano

A seção 4.6 do plano manda “ao abrir um fluxo, enumerar todos os diários B111”. A
redação correta, à luz das medições, é: resolver pelo nome determinístico no índice
existente; reler por GUID durante o fluxo; enumerar por prefixo **apenas** no caminho de
recuperação.

---

## 5. Achado colateral — cobertura de instrumentação

Levantamento estático do repositório, na mesma rodada:

- 89 chamadas a `GetAll` em `Src/`, das quais **20** passam por `ApiPlanScanProbe`;
- `ApiPlanGeneratedApiRemover.cs` concentra cerca de 15 delas, várias no padrão
  `GetAll(...).Any(...)`.

**Correção de uma contagem anterior.** Este documento chegou a registrar “56 chamadas a
`.Save()` em `Src/`, sendo 30 apenas em `ApiPlanBusinessComponentWriter.cs`”. A contagem
por texto era enganosa: a maior parte daquelas 30 ocorrências é **código GeneXus emitido
como string** (`lines.Add($"...{bc}.Save()")`), não gravação na KB.

Excluídas as sondas e as emissões de texto, os `Save()` **reais** dos writers de produção
são poucos e concentrados:

| Arquivo | `Save()` reais |
|---|---|
| ApiPlanSdtWriter.cs | 3 |
| ApiPlanListProcedureWriter.cs | 3 |
| ApiPlanBusinessComponentWriter.cs | 3 |
| ApiPlanProcedureWriter.cs | 2 |
| ApiPlanApiObjectWriter.cs | 2 |
| ApiPlanTransactionFolder.cs, ApiPlanMetadataFileWriter.cs, Package.cs | 1 cada |
| **total do pipeline** | **16** |

Isso reduz de forma significativa o tamanho estimado de um futuro seam de persistência:
são cerca de 16 pontos de gravação a interceptar, não 43. O número de Saves **executados**
numa aplicação continua maior que 16, porque vários desses pontos rodam em laço — um por
SDT, um por Procedure.

---

## 6. Efeitos consolidados sobre o plano de B111

1. Identidade planejada passa a ser o GUID lido do `Create`; some o ramo alternativo.
2. Some o marcador de identidade na `Description` e o helper associado.
3. A recuperação de Save indeterminado vira leitura por chave.
4. O diário é resolvido pelo índice na abertura e pelo **`Id`** durante o fluxo; nunca
   por remonte de índice e nunca por `Guid`, que `WikiFileKBObject.Get` não aceita.
5. O custo do diário entra no plano como orçamento medido de **~1,1 s por gravação na KB
   grande**, igual para criação e atualização, indiferente ao payload e específico do
   tipo `File`, o que obriga a agrupar atualizações em checkpoints em vez de uma por
   etapa.
6. O truncamento silencioso da `Description` sai desta frente e vira `B112`.

## 7. Dois achados fora do escopo de B111

### 7.1 Ocorrência espontânea do sintoma de B109, fora da etapa de Business Component

Na segunda execução sobre a KB grande, a sonda abortou com
`Collection was modified; enumeration operation may not execute` — o mesmo sintoma que
`B109` registra como intermitente na etapa de Business Component. Ocorreu no **primeiro
`Save()` de um API Object recém-criado**, num caminho que não envolve Business Component,
List, Procedures nem metadata.

Isso amplia o escopo conhecido de `B109`: o sintoma não é exclusivo da etapa de BC.

A hipótese levantada na ocasião — de que a atribuição do `Guid` por reflexão logo antes
do `Save()` seria a causa — foi testada de propósito em S1b, em quatro execuções sobre as
duas KBs. **Não se sustentou:** a atribuição foi aceita e o `Save()` concluiu sem erro em
todas elas, em 40 a 116 ms. A ocorrência permanece intermitente e sem causa identificada,
consistente com o registro original de `B109`.

### 7.2 Stall esporádico de dezenas de segundos num Save trivial

Na segunda execução sobre a KB grande, um `Save()` de API com Description de 120
caracteres — operação que custou 25 a 80 ms em todas as outras execuções — levou
**32.206 ms**. Nada no roteiro da sonda diferia; foi a mesma chamada, sobre o mesmo tipo
de objeto, momentos antes de gravações de 25 ms.

O dado importa para o entendimento de desempenho do projeto: além do custo médio, existe
uma **cauda** de stalls de dezenas de segundos em gravações triviais. Um único episódio
desses dentro de um Apply explica boa parte de uma execução percebida como travada, e
nenhuma otimização de média o elimina. A causa não foi investigada.

## 8. Medições em aberto

- Não foi isolado o que torna o `WikiFileKBObject` uma ordem de grandeza mais caro que os
  demais tipos, nem por que ele escala com o tamanho da KB enquanto os outros não.
- Não foi medido o custo de gravação da metadata B060 real, que é do mesmo tipo `File`.
- Não foi medido o custo de `transaction.Save()`, pelo motivo declarado na sonda: a
  extensão nunca cria Transaction, e o caso real incide sobre objeto pré-existente.
- Não foi explicada a única execução em que o File custou 21–147 ms na KB grande.
- Não foi medido se o setter de `Guid` é público em tempo de compilação: a sonda usou
  reflexão com `BindingFlags.Public | NonPublic`. Isso não afeta o desenho recomendado,
  que é ler o `Guid` do `Create` e nunca atribuí-lo.
