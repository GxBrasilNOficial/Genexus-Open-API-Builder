# B131 — Aceite na IDE: relatório do `Sincronizar` sem diferenças

- **Datas das sessões:** 2026-10-23 (cenário 1) e 2026-10-24 (cenário 2, reconferências por leitura e execução com Wizard seguido de Sync na `Tributacao`).
- **Item:** `B131` (`Docs/Foundation/06-BACKLOG_v0.1.md`, nota operacional de 2026-09-25).
- **Plano:** [2026-09-27-B131-PLANO-SYNC-SEM-DIFERENCA.md](2026-09-27-B131-PLANO-SYNC-SEM-DIFERENCA.md), versão v6.
- **Commit da DLL medida:** `6fb98d0` (Commit A). A DLL foi compilada a partir dele, com árvore limpa.
- **DLL:** `GenexusOpenApiBuilder.Extension.dll`, 1 358 848 bytes, SHA-256 `091C6676A98974BDE258A282AD6949FD9C9C5121C481A49B26B56180FFF229DC`.
- **Instalação:** conferida em 2026-10-23 por `Tools/Test-InstalledExtension.ps1`, com `InstalledMatchesBuild = True`. Foi só troca de DLL, sem `genexus /install`, porque manifesto e registro não mudaram.
- **GeneXus:** 18.0.15.188745 (U15).
- **Idioma:** português nas duas KBs, confirmado pelo operador antes de cada cenário.
- **Resultado:** cenário 1 **PASS**. Cenário 2 **PASS com ressalvas**, listadas na seção 4. A D6 fica cumprida com a `Empresa`, nas duas execuções. A condição registrada em 2026-09-25 (Wizard seguido de Sync sem diferença) foi reproduzida na `Tributacao` (seção 3A), sem ressalva sobre o Wizard.

## 1. Proveniência e alcance

- O que foi testado na IDE é a DLL do commit `6fb98d0`. A DLL satélite U13 foi compilada, mas não foi testada na IDE.
- Fora da cobertura: Sync com diferença (caminho não alterado) e renderização em espanhol e inglês.
- Campo não produzido: o tempo do diálogo na execução B do cenário 2 não foi registrado.

## 2. Cenário 1 — `NotaFiscal` (`wsEducacaoSpTeste`)

Execução única, em 2026-10-23.

| Item | Esperado | Obtido |
|---|---|---|
| `[B082] Sync PreviewMs=N` | presente | `N = 248` |
| `[B085] Diff` | sem delta | Adicionados 0, Removidos 0, Renomeados 0, Modificados 0, Inalterados 11 |
| `[B085] Nenhuma diferenca …` | presente | presente |
| `Resultado` | `Success` | `Success` |
| Criados / Atualizados / Removidos / Bloqueados | 0 | 0 |
| Avisos / Informações | 0 / 1 | 0 / 1 |
| `DuraçãoMs` igual a `PreviewMs` | igualdade exata | 248 = 248 |
| Linha `[B081] Aviso:` | ausente | ausente |
| Título | «Nenhuma sincronizacao necessaria.» | igual |
| Diálogo: cabeçalho, Informações (1), Avisos (nenhum), Tempo | presentes | presentes; `Tempo: 248 ms` (formato em ms, esperado abaixo de 1 s) |
| Botão «Abrir objeto principal» | registrar | aparece |
| `ApiSaveAttempted` / `ApiSaveCount` | `False` / 0 | `False` / 0 |

Output transcrito pelo operador:

```text
[Genexus Open API Builder][B082] Sync PreviewMs=248.
[Genexus Open API Builder][B085] Diff para Transaction='NotaFiscal':
Adicionados: 0; Removidos: 0; Renomeados: 0; Modificados: 0; Inalterados: 11
[Genexus Open API Builder][B085] Nenhuma diferenca entre Transaction e metadata. Nenhuma alteracao foi feita na KB.
[Genexus Open API Builder][B081] Relatório final: Operação='Sincronizar', Transaction='NotaFiscal', ApiName='apiNotaFiscal', PlannedApiName='apiNotaFiscal', PersistedMainObjectName='', PersistedMainObjectGuid='', FinalApiWriter='', ApiSaveAttempted=False, ApiSaveCount=0, Resultado='Success', Criados=0, Atualizados=0, Removidos=0, Bloqueados=0, Avisos=0, Informações=1, PersistenceReceipts=0, PersistenceStageFailures=0, DuraçãoMs=248, Título='Nenhuma sincronizacao necessaria.'.
```

**Estado da KB.** Antes e depois: `apiNotaFiscal` e `apiNotaFiscal_Metadata` com `lastUpdate` 2026-09-24. A Transaction `NotaFiscal` tem `lastUpdate` 2026-10-17, depois da API, e mesmo assim não houve diferença de contrato. Limpeza manual: não aplicável, porque o caminho não grava. `OperationId`: não aplicável, porque o caminho não abre diário. O GUID do objeto principal não é exposto pelo relatório (`PersistedMainObjectGuid` vazio), e o botão «Abrir objeto principal» aparece.

**Tentativa anterior na `Teste` (`apiTeste`), em 2026-10-23.** Não conta como execução do cenário. O Sync foi interrompido antes do ramo sem diferença, porque a metadata `apiTeste_Metadata` foi reconstruída pela recuperação de metadata órfã e não tem o contrato. O Output mostrou `Resultado='Interrupted'` e `DuraçãoMs=0`, e a mensagem de bloqueio foi impressa com o bloco `[B109]`, porque o bloqueio sai como exceção. O plano manda registrar e tentar outra Transaction, e foi o que se fez.

## 3. Cenário 2 — `Empresa` (`fabricabrasil18test`)

Duas execuções, em 2026-10-24. A primeira foi com o diálogo fechado antes da cópia do Output; a segunda foi relida depois da reabertura.

| Item | Esperado | Execução A | Execução B |
|---|---|---|---|
| `Kb Language` | português | português | português |
| `PreviewMs` | `1000 ≤ N < 60000` | 5485 | 3868 |
| `DuraçãoMs` igual a `PreviewMs` | igualdade exata | 5485 = 5485 | 3868 = 3868 |
| `Resultado` | `Success` | `Success` | `Success` |
| Avisos / Informações | 0 / 1 | 0 / 1 | 0 / 1 |
| Inalterados | sem delta | 221 | 221 |
| Criados / Atualizados / Removidos / Bloqueados | 0 | 0 | 0 |
| Tempo do diálogo | em segundos | `Tempo: 5,5 s` | não registrado |
| Cabeçalho do diálogo | «Nenhuma sincronizacao necessaria.» | Título do Output confere; captura não recebida | Título do Output confere; captura não recebida |
| Botão «Abrir objeto principal» | registrar | não verificado, porque o diálogo foi fechado antes da cópia do Output | aparece |
| `ApiSaveAttempted` / `ApiSaveCount` | `False` / 0 | `False` / 0 | `False` / 0 |

Output da execução A, transcrito pelo operador:

```text
[Genexus Open API Builder][B082] Sync PreviewMs=5485.
[Genexus Open API Builder][B085] Diff para Transaction='Empresa':
Adicionados: 0; Removidos: 0; Renomeados: 0; Modificados: 0; Inalterados: 221
[Genexus Open API Builder][B085] Nenhuma diferenca entre Transaction e metadata. Nenhuma alteracao foi feita na KB.
[Genexus Open API Builder][B081] Relatório final: Operação='Sincronizar', Transaction='Empresa', ApiName='apiEmpresa', PlannedApiName='apiEmpresa', PersistedMainObjectName='', PersistedMainObjectGuid='', FinalApiWriter='', ApiSaveAttempted=False, ApiSaveCount=0, Resultado='Success', Criados=0, Atualizados=0, Removidos=0, Bloqueados=0, Avisos=0, Informações=1, PersistenceReceipts=0, PersistenceStageFailures=0, DuraçãoMs=5485, Título='Nenhuma sincronizacao necessaria.'.
```

Output da execução B, transcrito pelo operador:

```text
[Genexus Open API Builder][B082] Sync PreviewMs=3868.
[Genexus Open API Builder][B085] Diff para Transaction='Empresa':
Adicionados: 0; Removidos: 0; Renomeados: 0; Modificados: 0; Inalterados: 221
[Genexus Open API Builder][B085] Nenhuma diferenca entre Transaction e metadata. Nenhuma alteracao foi feita na KB.
[Genexus Open API Builder][B081] Relatório final: Operação='Sincronizar', Transaction='Empresa', ApiName='apiEmpresa', PlannedApiName='apiEmpresa', PersistedMainObjectName='', PersistedMainObjectGuid='', FinalApiWriter='', ApiSaveAttempted=False, ApiSaveCount=0, Resultado='Success', Criados=0, Atualizados=0, Removidos=0, Bloqueados=0, Avisos=0, Informações=1, PersistenceReceipts=0, PersistenceStageFailures=0, DuraçãoMs=3868, Título='Nenhuma sincronizacao necessaria.'.
```

**Estado da KB.** Antes das execuções: `Empresa` (Transaction) com `lastUpdate` 2026-06-30; `apiEmpresa` com 2026-09-27T22:15:48Z; `apiEmpresa_Metadata` com 2026-09-27T22:15:50Z. Verificado em 2026-10-24, depois das duas execuções, por leitura no MCP: nenhuma mudou. O GUID do objeto principal não é exposto pelo relatório.

## 3A. Execução com Wizard seguido de Sync imediato — `Tributacao` (`fabricabrasil18test`)

Execução do operador em 2026-10-24. A Transaction `Tributacao` não tinha API. O Wizard criou a API e a metadata, e logo depois o Sincronizar foi rodado. Essa é a condição do registro de 2026-09-25 («logo depois de um Wizard»), com a sequência conhecida. O Wizard gravou na KB; essa escrita foi ação do operador, que é o mantenedor deste plano. A ressalva sobre o Wizard da `Empresa` (seção 4) não se aplica a esta execução.

Relatório final do Wizard, transcrito pelo operador (sem critério do B131; registrado para a sequência):

```text
[Genexus Open API Builder][B081] Relatório final: Operação='Wizard', Transaction='Tributacao', ApiName='apiTributacao', PlannedApiName='apiTributacao', PersistedMainObjectName='apiTributacao', PersistedMainObjectGuid='595a992a-03e0-4f79-8110-6d61df11c840', FinalApiWriter='List', ApiSaveAttempted=True, ApiSaveCount=1, Resultado='SuccessWithWarnings', Criados=33, Atualizados=3, Removidos=0, Bloqueados=0, Avisos=1, Informações=0, PersistenceReceipts=37, PersistenceStageFailures=0, DuraçãoMs=38910, Título='API gerada com avisos.'.
[Genexus Open API Builder][B081] Aviso: Descricoes de servico usaram fallback em ingles (idioma da KB ainda nao validado por API publica).
```

O aviso do Wizard é de descrições em inglês e não tem relação com o B131.

Sincronizar imediato, Output transcrito pelo operador:

```text
[Genexus Open API Builder][B082] Sync PreviewMs=2668.
[Genexus Open API Builder][B085] Diff para Transaction='Tributacao':
Adicionados: 0; Removidos: 0; Renomeados: 0; Modificados: 0; Inalterados: 91
[Genexus Open API Builder][B085] Nenhuma diferenca entre Transaction e metadata. Nenhuma alteracao foi feita na KB.
[Genexus Open API Builder][B081] Relatório final: Operação='Sincronizar', Transaction='Tributacao', ApiName='apiTributacao', PlannedApiName='apiTributacao', PersistedMainObjectName='', PersistedMainObjectGuid='', FinalApiWriter='', ApiSaveAttempted=False, ApiSaveCount=0, Resultado='Success', Criados=0, Atualizados=0, Removidos=0, Bloqueados=0, Avisos=0, Informações=1, PersistenceReceipts=0, PersistenceStageFailures=0, DuraçãoMs=2668, Título='Nenhuma sincronizacao necessaria.'.
```

Diálogo do relatório, pela captura do operador: cabeçalho «Nenhuma sincronizacao necessaria.»; `Operação: Sincronizar`; `Transaction: Tributacao`; `API: apiTributacao`; `Resultado: Criados=0; Atualizados=0; Removidos=0.`; `Tempo: 2,7 s`; seção «Informações (1)» com a frase de ausência de diferença; `Avisos: (nenhum)`. Os botões «Abrir objeto principal» e «Fechar» aparecem.

| Item | Esperado | Obtido |
|---|---|---|
| `PreviewMs` | `1000 ≤ N < 60000` | 2668 |
| `DuraçãoMs` igual a `PreviewMs` | igualdade exata | 2668 = 2668 |
| `Resultado` | `Success` | `Success` |
| Avisos / Informações | 0 / 1 | 0 / 1 |
| Inalterados | sem delta | 91 |
| Tempo do diálogo | em segundos | `Tempo: 2,7 s` |
| Cabeçalho do diálogo | «Nenhuma sincronizacao necessaria.» | confere (captura) |
| Botão «Abrir objeto principal» | registrar | aparece (captura) |
| `ApiSaveAttempted` / `ApiSaveCount` | `False` / 0 | `False` / 0 |

**Estado da KB.** Lido pelo MCP em 2026-10-24, depois do Sync: `apiTributacao` (API) com `lastUpdate` 2026-10-24T18:38:51Z; `apiTributacao_Metadata` com 2026-10-24T18:38:52Z. Nenhuma mudou depois dessa data, então o Sync não gravou nada. A Transaction `Tributacao` tem `lastUpdate` 2026-07-01T19:17:38Z. O horário exato do Sync não foi registrado: ele foi rodado depois do Wizard, e a metadata não mudou depois de 18:38:52 UTC. `OperationId` e limpeza manual: não aplicáveis ao Sync.

## 4. Ressalvas do cenário 2

1. **Relação com o Wizard não confirmada.** O último save do `apiEmpresa` foi em 2026-09-27 às 22:15:48, e a metadata 2 segundos depois. Esse padrão é compatível com uma aplicação completa (Wizard), e não com recuperação de metadata órfã, que regrava só a metadata. O operador não tem a data exata do último Wizard. O registro de 2026-09-25 descreve a Empresa «logo depois de um Wizard», e essa relação não foi confirmada. O operador aceitou a Empresa com essa ressalva em 2026-10-24. A execução com a `Tributacao` (seção 3A) reproduz essa condição com a sequência conhecida; por isso a ressalva vale só para a `Empresa`.
2. **Cabeçalho do diálogo não transcrito** nas duas execuções. O Título do Output confere com a frase esperada, mas a captura do cabeçalho não foi recebida. Pendência declarada apenas para a `Empresa`; na `Tributacao` (seção 3A), a captura mostra o cabeçalho.
3. **Botão «Abrir objeto principal»:** aparece na execução B. Na execução A, o diálogo foi fechado antes da cópia do Output, então não foi verificado. Ao clicar, o objeto abre por baixo do relatório, e só fica visível depois de fechar o relatório. Isso não é critério do B131 e fica como observação de usabilidade.
4. **Tempo do diálogo na execução B** não foi registrado. O `DuraçãoMs` do Output é 3868.

## 5. Medição da igualdade `DuraçãoMs` × `PreviewMs` (seção 8.2)

Medição refeita em 2026-10-22, em processo próprio, fora da IDE, com o `csc.exe` do .NET Framework (runtime `v4.0.30319`) nas arquiteturas x86 e x64. O programa completo está transcrito abaixo. Ele foi escrito nesta sessão e compilado com:

- `csc.exe` de `C:\Windows\Microsoft.NET\Framework64\v4.0.30319` para x64;
- `csc.exe` de `C:\Windows\Microsoft.NET\Framework\v4.0.30319` para x86.

```csharp
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

// B131 (plano secao 8.2): igualdade exata entre DuracaoMs do resumo e PreviewMs.
// Compara ticks / TicksPerMillisecond com (int)new TimeSpan(ticks).TotalMilliseconds
// nas bordas de cada milissegundo e compara ElapsedMilliseconds com
// (int)Elapsed.TotalMilliseconds em Stopwatch reais, lidos do mesmo estado parado.
internal static class Medicao
{
    private static int Main()
    {
        const long kMax = 3999999;
        long casosTicks = 0;
        long divergenciasTicks = 0;
        long casosStopwatch = 0;
        long divergenciasStopwatch = 0;
        int exibidas = 0;

        for (long k = 0; k <= kMax; k++)
        {
            long baseTicks = 10000L * k;
            long[] candidatos = { baseTicks - 1, baseTicks, baseTicks + 1, baseTicks + 9999 };
            foreach (long ticks in candidatos)
            {
                casosTicks++;
                long esperado = ticks / TimeSpan.TicksPerMillisecond;
                long obtido = (int)new TimeSpan(ticks).TotalMilliseconds;
                if (esperado != obtido)
                {
                    divergenciasTicks++;
                    if (exibidas < 10)
                    {
                        Console.WriteLine("DIVERGENCIA ticks={0} esperado={1} obtido={2}", ticks, esperado, obtido);
                        exibidas++;
                    }
                }
            }
        }

        var random = new Random(20261022);
        for (int i = 0; i < 2000; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            Thread.SpinWait(random.Next(1, 3000000));
            stopwatch.Stop();
            casosStopwatch++;
            long ms = stopwatch.ElapsedMilliseconds;
            long ms2 = (int)stopwatch.Elapsed.TotalMilliseconds;
            if (ms != ms2)
            {
                divergenciasStopwatch++;
                Console.WriteLine("DIVERGENCIA stopwatch ElapsedMilliseconds={0} Elapsed.TotalMilliseconds={1}", ms, ms2);
            }
        }

        // Controle positivo: o comparador tem de acusar diferenca quando ela existe.
        long controleDetectadas = 0;
        for (long k = 0; k <= 999; k++)
        {
            long ticks = 10000L * k;
            long esperado = ticks / TimeSpan.TicksPerMillisecond + 1;
            long obtido = (int)new TimeSpan(ticks).TotalMilliseconds;
            if (esperado != obtido)
            {
                controleDetectadas++;
            }
        }

        Console.WriteLine("runtime={0} processo={1} bits", RuntimeEnvironment.GetSystemVersion(), IntPtr.Size * 8);
        Console.WriteLine("casos_ticks={0} divergencias_ticks={1}", casosTicks, divergenciasTicks);
        Console.WriteLine("casos_stopwatch={0} divergencias_stopwatch={1}", casosStopwatch, divergenciasStopwatch);
        Console.WriteLine("casos_total={0} divergencias_total={1}", casosTicks + casosStopwatch, divergenciasTicks + divergenciasStopwatch);
        Console.WriteLine("controle_positivo_detectadas={0} de 1000", controleDetectadas);
        return divergenciasTicks + divergenciasStopwatch == 0 && controleDetectadas == 1000 ? 0 : 1;
    }
}
```

Saída x64:

```text
runtime=v4.0.30319 processo=64 bits
casos_ticks=16000000 divergencias_ticks=0
casos_stopwatch=2000 divergencias_stopwatch=0
casos_total=16002000 divergencias_total=0
controle_positivo_detectadas=1000 de 1000
```

Saída x86:

```text
runtime=v4.0.30319 processo=32 bits
casos_ticks=16000000 divergencias_ticks=0
casos_stopwatch=2000 divergencias_stopwatch=0
casos_total=16002000 divergencias_total=0
controle_positivo_detectadas=1000 de 1000
```

A contagem do plano dizia 16 001 999 casos. O método dá 4 000 000 × 4 + 2 000 = 16 002 000. O plano foi corrigido na seção 8.2 e na seção 14, por decisão do mantenedor.

## 6. Gate `tests.applicationFinalReport` (seção 7.1, item 3)

- `Tests/ApplicationFinalReport/Test-ApiPlanApplicationFinalReport.ps1` retornou `Test-ApiPlanApplicationFinalReport.ps1 OK`, com saída `0`. O autoteste embutido, com o modelo correto, os defeitos A1 a A5, o comentário e o `AddInformation` fora do ramo, passou.
- Controle negativo: o mesmo gate, apontado para o `Package.cs` anterior ao B131 (`git show` do commit `d570163`), reprovou com saída `1`. A primeira mensagem foi `A1: previewElapsed = previewWatch.Elapsed deve vir depois de previewWatch.Stop().`. O gate para na primeira trava que falha, então A2 a A5 também reprovam, mas não aparecem nessa saída.

## 7. Rastreabilidade

Saídas literais, em 2026-10-24:

```text
$ git log -S 'noOpReport.AddWarning' --oneline -- Src/Extension/Package.cs
6fb98d0 B131: relatório do Sincronizar sem diferenças sai como sucesso
abb585b feat(report): relatório final pós-aplicação (B081) validado no U15

$ git log -S 'ShowFinalReport(noOpReport, TimeSpan.Zero' --oneline -- Src/Extension/Package.cs
6fb98d0 B131: relatório do Sincronizar sem diferenças sai como sucesso
abb585b feat(report): relatório final pós-aplicação (B081) validado no U15

$ git merge-base --is-ancestor abb585b v0.1.0-alpha.1 ; echo $?
0

$ git merge-base --is-ancestor 6e8b28f v0.1.0-alpha.1 ; echo $?
1

$ git tag --contains abb585b
v0.1.0-alpha.1
v0.1.0-alpha.2
v0.1.0-alpha.3
v0.1.0-alpha.4
v0.1.0-alpha.5
v0.1.0-alpha.6
v0.1.0-alpha.7
v0.1.0-alpha.8
v0.1.0-alpha.9

$ git tag --contains 6e8b28f
v0.1.0-alpha.3
v0.1.0-alpha.4
v0.1.0-alpha.5
v0.1.0-alpha.6
v0.1.0-alpha.7
v0.1.0-alpha.8
v0.1.0-alpha.9
```

A origem do defeito é `abb585b`, que está em todas as Alphas publicadas, da `0.1.0-alpha.1` à `0.1.0-alpha.9`. O commit `6fb98d0` aparece nas duas buscas `-S` porque é o commit que remove essas strings. O commit `6e8b28f`, que trouxe os rótulos `Resultado=`, `Avisos=` e `DuraçãoMs=`, não é ancestral da `0.1.0-alpha.1`, e por isso o `CHANGELOG` diz que a entrada vale a partir da `alpha.3`.

Outros registros:
- Plano: `Docs/Implementation/2026-09-27-B131-PLANO-SYNC-SEM-DIFERENCA.md`, seções 5, 8.2 e 14.
- Checkpoint: `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`, item 189 do log (Commit A) e item 190 (fechamento).
- `CHANGELOG.md`, seção `[Unreleased]`, `### Fixed`, entrada `B131`.

## 8. Pendências e observações (fora do critério do B131)

1. **D4 — `catch` externo.** Nas falhas, o `DuraçãoMs` do relatório sai zero, como na execução bloqueada da `Teste`. É um caso anterior ao B131 e fica fora do escopo. Cabe ao mantenedor decidir se abre item de backlog.
2. **D5 — acentuação em pt-BR.** Não alterada, como decidido.
3. **Rótulo `Informações=`** no resumo da Output não tem entrada no catálogo (seção 8.3 do plano).
4. **Registro de 2026-08-08 do `B085`** diz «sem janela» para o Sync sem mudança. Não foi editado, porque é registro datado.
5. **Renderização em espanhol e inglês:** não coberta.
6. **DLL satélite U13:** só a compilação foi verificada.
7. **Sync com diferença:** caminho não alterado e não testado.
8. **Bloqueio por metadata órfã** sai como exceção e imprime o bloco `[B109]`. O plano prevê esse bloco só em falha de etapa. Fica registrado.
9. **Transaction `NotaFiscal`** foi alterada em 2026-10-17, depois da API, sem diferença de contrato.
10. **Botão «Abrir objeto principal»:** abre o objeto por baixo do relatório, que precisa ser fechado para ser visto.
