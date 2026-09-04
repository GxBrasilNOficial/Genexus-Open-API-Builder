# Evidência de linha de base na IDE — drift do API Object (2026-09-03/04)

## Propósito

Registrar, com medição direta na IDE, o comportamento **atual** da ferramenta nos pontos que a
frente de reordenação de gravação do API Object pretende alterar. As capturas foram feitas
**antes de qualquer mudança de código**, para servir de linha de base comparável.

O plano dessa frente ainda não está no repositório; está sendo elaborado em revisão por pares.
Este documento é independente dele: descreve o que a ferramenta faz hoje, não o que se propõe
mudar.

Tudo abaixo é observação direta, salvo os trechos marcados como INTERPRETAÇÃO.

## 1. Ambiente

- Commit `4b9ca09`, working tree limpa.
- DLL compilada em 2026-09-03 17:38; a instalada em `Packages\` é idêntica byte a byte.
  SHA-256 `ABB79248F93BB818E4BD8382506813B1A637BF607C880A82ECC3A80A7B4A7624`.
- KB `FabricaBrasil18Test` (`C:\GxModels\FabricaBrasil18Test`).
- Transaction `Empresa`, `Root Module`, Business Component habilitado.
- Porte: PK=1 (`EmpresaId`), Create=102, Update=102, Response=162, ListFilters=1, 44 SDTs
  próprios + 3 compartilhados, com subníveis (B096). Para comparação, a `NotaFiscal` usada na
  validação histórica do projeto tem 8 SDTs.
- Folder `EmpresaOpenApi` preexistente, reutilizado (aviso em todas as execuções).

## 2. Execução 1 — primeira geração completa

Ordem observada: SDTs → Procedures → API Object → Business Component → List → Metadata.

| Fase | ms |
|---|---|
| IndiceKb | 2.816 |
| PreflightAgregado | 2 |
| SDTs | 46.951 |
| IndiceSdtAposGravacao | 164 |
| Procedures | 1.389 |
| ApiObject | 2.907 |
| BusinessComponent | 38.515 |
| List | 8.892 |
| Metadata | 2.090 |
| **Total** | **103.790** |

Resultado: `SuccessWithWarnings`, Criados=50, Bloqueados=0, Avisos=2.

- API Object `apiEmpresa`, GUID `c1d0ae0f-6af5-4e7f-9c2e-09c0bdf0ef2c`
- Metadata `apiEmpresa_Metadata`, GUID `1f302af6-d531-4dc8-ad31-12da704c9e0f`, Bytes=1.344.663,
  Sha256 `202A62EF757EEA1194BA44A9D05DFFD970B49E31BDEFF10F82CD0896D5ED8439`
- `PlannedContractHash` `41DBB659E224E87498002E270574F7BBA0DEAAD99120F4BFE585BA415DA9E12F`

**Medição central:** entre o fim da gravação do API Object e a gravação da metadata há
38.515 + 8.892 + 2.090 ≈ **49 segundos** em que o API Object está adiante do baseline.

## 3. Execução 2 — falha espontânea da etapa de Business Component

Reaplicação limpa, minutos depois, sem conflito e sem cancelamento:

```
[B071-B073/B079] ... Error='Exception has been thrown by the target of an invocation.
| Inner='Collection was modified; enumeration operation may not execute.''
```

- `Fase BusinessComponent=5.200 ms` (contra 38.515 na execução completa).
- `[B060] Metadata nao foi gravada ... porque B071-B073/B079 falhou`.
- Relatório: `Interrupted`, Bloqueados=1 (`Business Component / REST`).

Leitura direta da KB confirmou que o `apiEmpresa` continuava com
`List(in: &ApiPage, in: &ApiPageSize, in: &EmpresaId, …)` — a forma final da geração completa.
**O API Object não foi tocado**, e não houve divergência.

**Bug real e intermitente, não relacionado à frente de ordenação.** Não se reproduziu na
execução seguinte, com a mesma Transaction e a mesma operação. Merece item de backlog próprio.
Hipótese não verificada: pode depender da escala (47 SDTs, subníveis), já que toda a validação
histórica foi feita sobre uma Transaction com 8 SDTs.

## 4. Execução 3 — reaplicação bem-sucedida

Total 66.014 ms (BusinessComponent 43.925; List 8.699; Metadata 2.507).
Resultado `SuccessWithWarnings`, Atualizados=6, Bloqueados=0.

A metadata foi regravada com **mesmos** Bytes (1.344.663) e mesmo `PlannedContractHash`, porém
**Sha256 diferente**: `775B69F92B8C8942D6D5F0AB60AC4A3E945D0B47F3167B1DC91F49FCE7D12DA4`.

**Consequência para qualquer plano de validação futuro:** duas gravações do mesmo plano produzem
conteúdos distintos (provável carimbo de tempo interno). Portanto **"o Sha256 do File mudou" não
prova mudança de contrato** — prova apenas que houve regravação. O critério utilizável é
"a metadata não foi regravada", lido da Output.

## 5. Execução 4 — cancelamento precoce, inócuo

Cancelamento durante a etapa de API Object. A Output registra `Fase ApiObject=3 ms` e, em
seguida, o aborto — **sem** `Fase BusinessComponent`. Total 16.039 ms. O aborto foi capturado no
ponto de verificação entre etapas e o API Object não foi tocado.

Nota: o relatório exibe "a KB pode ter ficado inconsistente. Use Remover / Wizard / Sync para
reparar" mesmo quando nada ficou inconsistente. O aviso é conservador por construção.

## 6. Execução 5 — cancelamento após `2/4`: drift produzido

A janela de andamento mostra, dentro da etapa de Business Component, contador e nome do objeto
em gravação:

```
1/4  apiEmpresa
2/4  procEmpresa_API_Get
3/4  procEmpresa_API_Create
4/4  procEmpresa_API_Update
```

Cancelamento acionado logo após aparecer `2/4` — portanto **depois** de o passo 1 concluir a
gravação do API Object. Total 29.768 ms; a linha `Fase BusinessComponent=` não chegou a ser
emitida porque a exceção de aborto propagou antes dela.

Leitura direta da KB depois do aborto:

```
List()
    => procEmpresa_API_List();
```

Contra o estado anterior:

```
List(in: &ApiPage, in: &ApiPageSize, in: &EmpresaId, out: &ListResponse, out: &ErrorResponse)
    => procEmpresa_API_List(&ApiPage, &ApiPageSize, &EmpresaId, &ListResponse, &ErrorResponse, &RestStatusCode);
```

O API Object ficou na variante gravada pela etapa de Business Component, que emite o serviço
List na forma degenerada, sem parâmetro algum — enquanto `procEmpresa_API_List` tem seis. A
metadata permaneceu no baseline anterior.

**Medição:** o estado intermediário não é apenas incompleto, é **inconsistente** — o API Object
delega com aridade incompatível com a Procedure. Isso ocorre no caminho **atual** da ferramenta.

## 7. Execução 6 — o bloqueio, e a cascata de degradação

### 7.1 O bloqueio

```
Causa='BaselineServiceSourceHashMismatch'
BaselineVersionOk=True
BaselineGuidOk=True
BaselineDescriptionOk=True
BaselineServiceSourceHashOk=False        <- única cláusula que falha
BaselineServiceDescriptionsHashOk=True
ServiceSourceHashAtual  ='A2956241F15BCD2D6352ADD5811EA4ABE3318E3066135C114C7A95C5CE5BA580'
ServiceSourceHashGravado='F3075D925A3D42D4D4B6AAA0B9EB298A1AED3EFF3C7D315446449B2DBF277451'
FingerprintOk=True   (metadata íntegra; snapshot de 920.825 bytes)
```

Das cinco cláusulas do baseline B067, apenas o hash do Service Source diverge, e é ele que
bloqueia. O hash atual coincide com o token de versão obtido por leitura independente da KB.

### 7.2 A cascata de degradação

O bloqueio do API Object **não interrompeu a operação**. Ele desligou as etapas dependentes
(`GenerateApiObject=False, GenerateMetadata=False, ApplyList=False, ApplyBusinessComponent=False`)
e deixou as demais gravarem:

```
Relatório final: Resultado='SuccessWithWarnings', Criados=0, Atualizados=5, Bloqueados=0
  Atualizado: SDT       sdtEmpresa_API_ListFilters
  Atualizado: Procedure procEmpresa_API_List
  Atualizado: Procedure procEmpresa_API_Get
  Atualizado: Procedure procEmpresa_API_Create
  Atualizado: Procedure procEmpresa_API_Update
```

Mecanismo observado:

1. o Wizard monta o contrato lendo o API Object para descobrir os filtros existentes;
2. o API Object estava degenerado (`List()` sem parâmetros), então o plano veio com
   `ListFilters=0` — nas execuções anteriores vinha `ListFilters=1`;
3. o writer de SDT tratou o membro real como sobra —
   `SDT diverge: Name='sdtEmpresa_API_ListFilters', Motivo='membros extras 'EmpresaId''` —
   e regravou o SDT **sem** o membro;
4. as quatro Procedures foram regravadas conforme esse contrato empobrecido.

**Consequência:** a tentativa de recuperação natural — reabrir o Wizard e aplicar — **piora a
KB**, em silêncio, sob um relatório que diz "API gerada com avisos" e contabiliza
`Bloqueados=0`. O único objeto protegido é justamente aquele que disparou o bloqueio.

**INTERPRETAÇÃO:** a descrição usual da dor ("trava e exige Remover + Wizard") é incompleta. O
enunciado correto é: trava o API Object, e a tentativa seguinte **propaga o estado corrompido**
para SDTs e Procedures que ainda estavam íntegros. Isto é relevante para qualquer frente que
mexa na ordem de gravação, e também, independentemente dela, para o desenho do próprio
comportamento de bloqueio parcial.

## 8. Estado degradado e reparo

Ao fim das capturas a `Empresa` estava **degradada**:

- `apiEmpresa` com `List()` sem parâmetros;
- `sdtEmpresa_API_ListFilters` sem o membro `EmpresaId`;
- quatro Procedures regravadas conforme o contrato empobrecido;
- metadata ainda no baseline da geração íntegra.

### 8.1 Reparo executado — Remover API gerada

O comando de remoção resolveu o estado sem intercorrências:
`Resultado='Success'`, Removidos=50, Bloqueados=0, **Avisos=0**.

Preservou corretamente o que devia preservar: os três SDTs compartilhados
(`sdt_API_ErrorMessage`, `sdt_API_ErrorResponse`, `sdt_API_Pagination`), o Folder
`EmpresaOpenApi` (reutilizado, `FolderWasCreated=False`) e o Business Component da Transaction.

Vale registrar que a remoção funcionou **a partir do estado degradado**, inclusive com o API
Object divergente do baseline: o plano de remoção é montado a partir da metadata, e a
divergência de Service Source não o impediu.

### 8.2 Custo da remoção — dado para a frente de desempenho

| Fase | ms |
|---|---|
| ResolucaoMetadata | 2.883 |
| ValidacaoAgregada | 0 |
| ApiObject | 334 |
| Procedures | 8.623 |
| Sdts | 31.244 |
| MetadataFile | 198 |
| **Total** | **43.339** |

`Scans=148, TotalScanMs=29.711` — **69% do tempo total em varreduras**. São três varreduras por
objeto (`localizacao-delete`, `revalidacao-pre-delete`, `confirmacao-pos-delete`), o que dá 132
varreduras só para os 44 SDTs:

```
SDT/revalidacao-pre-delete    44x, 7.731ms
SDT/confirmacao-pos-delete    44x, 7.259ms
SDT/localizacao-delete        44x, 7.152ms
Procedure/localizacao-delete   4x, 2.602ms
Procedure/confirmacao-pos-delete 4x, 2.489ms
Procedure/revalidacao-pre-delete 4x, 2.454ms
```

A meta da Etapa 1B do plano de desempenho (`2026-09-02-B082-PLANO-HARDENING-E-DESEMPENHO.md`)
para "Remove `Empresa`" é **≤ 20 s**. O medido hoje, sem nenhuma mudança aplicada, é **43,3 s** —
mais que o dobro. Medição de campo obtida incidentalmente durante estas capturas; a Etapa 1B não
foi executada, e este número é a linha de base contra a qual ela deverá ser comparada.

## 9. Itens que estas capturas levantam

1. **Cascata de degradação (7.2)** — comportamento não documentado até aqui. Independe da frente
   de ordenação e vale avaliação própria: um bloqueio parcial que permite às demais etapas
   gravarem a partir de um plano derivado do estado bloqueado.
2. **Bug intermitente** `Collection was modified; enumeration operation may not execute` na etapa
   de Business Component em reencontro (seção 3).
3. **Ordem de gravação sem registro:** a janela de andamento mostra a sequência ao vivo
   (contador e nome do objeto), mas nada disso chega à Output — não há como anexar a ordem
   efetiva a uma evidência depois do fato.
4. **Hash da metadata não é estável** entre gravações do mesmo plano (seção 4); qualquer critério
   de verificação que dependa disso está errado.
5. **Remoção na KB grande custa 43,3 s contra a meta de 20 s** da Etapa 1B do plano de
   desempenho, com 69% do tempo em varreduras e três varreduras por objeto removido (seção 8.2).

## 10. Estado final da KB

Íntegro. A `Empresa` está sem API gerada: os 50 objetos foram removidos, e restaram o Folder
`EmpresaOpenApi` (vazio, preservado por ser reutilizado), os SDTs compartilhados e o Business
Component da Transaction. Regerar exige o Wizard completo (~104 s na primeira geração, conforme
a seção 2).
