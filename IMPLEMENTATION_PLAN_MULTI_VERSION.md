# Plano: Suporte ao GeneXus 18 U13 (e versões anteriores)

## Contexto

O projeto **Genexus Open API Builder** (v0.1.0-alpha.1) foi desenvolvido e validado exclusivamente no **GeneXus 18 U15**, com o sistema de build baseado no **feed NuGet oficial** (`GeneXus.Package.UI.Sdk` v3.0.0-beta5) e pacotes referenciados na versão `18.13.2`.

O objetivo deste plano é identificar o que precisa ser feito para suportar o **GeneXus 18 U13 para trás**, definir até qual versão o suporte é viável, e detalhar as mudanças técnicas necessárias.

---

## Diagnóstico Geral — O que faz o projeto funcionar só no U14+

A razão primária pela qual a extensão só funciona a partir do U14 é **arquitetural/build**, não funcional:

> [!IMPORTANT]
> A partir do GeneXus 18 U14, a Genexus **descontinuou o instalador SDK legado** e passou a distribuir os assemblies de referência exclusivamente via **NuGet** (feed `genexus-build-sdk`). O projeto GOAB foi construído desde o início nesse modelo NuGet, usando `GeneXus.Package.UI.Sdk` como SDK MSBuild e pacotes `Artech.*` versão `18.13.2`.
>
> No U13 e anteriores, não existem esses pacotes NuGet — era necessário referenciar DLLs físicas do instalador SDK local.

---

## Análise de Compatibilidade por Camada

### Camada 1: Sistema de Build (BLOQUEANTE para U13-)

| Item | Status atual | Impacto para U13- |
|------|-------------|-------------------|
| `GeneXus.Package.UI.Sdk` (MSBuild SDK NuGet) | `3.0.0-beta5` | **Não existe no U13.** Precisa de abordagem alternativa |
| `GeneXusPackageReferenceVersion` = `18.13.2` | No `Directory.Build.props` | Versão que corresponde ao U13 seria `18.12.x` |
| Pacotes `Artech.*.Sdk` v`18.13.2` | Todos diretos via NuGet | Pacotes equivalentes `18.12.x` existem? Precisa verificar |
| Target Framework `net471` | Ok — não muda | Compatível com GX17–GX18 |
| `LangVersion: latest` + `Nullable: enable` | C# moderno | Pode ser problema em compiladores mais antigos |

**Conclusão:** Para U13 e anteriores, o modelo de build precisa mudar. Há dois caminhos:
- **Caminho A (NuGet):** Verificar se a GeneXus publicou pacotes `18.12.x` (U13) ou `18.11.x` (U12) no mesmo feed. Se sim, criar um segundo `Directory.Build.props` com a versão correta.
- **Caminho B (SDK Legado):** Usar o instalador SDK clássico do GX18 U13 e referenciar DLLs fisicamente — abandonando o modelo NuGet. Complexo, não recomendado para manutenção futura.

---

### Camada 2: APIs do Extensibility SDK (CRÍTICAS — verificar disponibilidade por versão)

As APIs usadas no GOAB e sua disponibilidade estimada:

#### 2.1 Classe `API` (Objeto API do GeneXus)

| API SDK usada | Arquivo | Disponível desde |
|--------------|---------|------------------|
| `API.GetAll(model)` | `ApiPlanApiObjectWriter.cs` | **GX 17 U1** (o objeto API foi introduzido nessa versão) |
| `API.Create(model)` | `ApiPlanApiObjectWriter.cs` | **GX 17 U1** |
| `API.Get(model, guid)` | `ApiPlanApiObjectWriter.cs` | **GX 17 U1** |
| `api.ServiceGroupSource.Source` | Múltiplos writers | **GX 17 U1** (parte fundamental do API Object) |
| `api.Events.Source` | `ApiPlanBusinessComponentWriter.cs` (linha 554) | **GX 17 U1** (Events é parte do API Object) |
| `api.Variables.Variables` | Múltiplos writers | **GX 17 U1** |
| `api.Parent = folder` | Múltiplos writers | **GX 17 U1** |

#### 2.2 Propriedade `idVarServiceRequired` (CRÍTICA — ponto mais arriscado)

```csharp
// ApiPlanBusinessComponentWriter.cs linha 20
internal const string ServiceRequiredPropertyId = "idVarServiceRequired";
// ...
variable.SetPropertyValue(ServiceRequiredPropertyId, true);
// ...
variable.ContainsPropertyDefinition(ServiceRequiredPropertyId)
```

Esta propriedade (`idVarServiceRequired`) controla o campo `Required: true` no YAML OpenAPI gerado. **Ela pode não existir em versões mais antigas do SDK.** O código já tem proteção:
```csharp
if (!spec.IsServiceRequired || !variable.ContainsPropertyDefinition(ServiceRequiredPropertyId))
    return; // pula silenciosamente se não existe
```
Portanto, se a propriedade não existir em uma versão mais antiga, o comportamento degrada graciosamente: a variável não será marcada como Required, mas a extensão não quebra.

#### 2.3 Anotação `[SecurityLevel(...)]` no ServiceGroupSource

```csharp
// ServiceAnnotations — linha 2322
annotations.Add($"    [SecurityLevel({plan.Security.SecurityLevel})]");
```

A anotação `[SecurityLevel]` é **textual** — é gerada como string no ServiceGroupSource, não via API SDK. Portanto, a disponibilidade depende de quando o GeneXus **interpretador** do ServiceGroupSource começou a suportar essa anotação.

> [!WARNING]
> O `[SecurityLevel]` no ServiceGroupSource foi introduzido **no GeneXus 17 U1** junto com o API Object. Porém, a aplicação programática dessa anotação via SDK (`variable.SetPropertyValue(...)` e similares) precisa ser verificada versão a versão.

#### 2.4 Outras APIs do SDK

| API SDK | Disponibilidade estimada |
|---------|--------------------------|
| `Transaction.GetAll(model)` | GX 17 U1+ (API pública estável) |
| `Transaction.IsBusinessComponent` | GX 17 U1+ |
| `SDT.GetAll(model)`, `SDT.Create(model)` | GX 17 U1+ |
| `Procedure.GetAll(model)`, `Procedure.Create(model)` | GX 17 U1+ |
| `Folder.GetAll(model)`, `Folder.Create(model)` | GX 17 U1+ |
| `WikiFileKBObject.GetAll(model)` (File metadata) | GX 17 U1+ (File é tipo clássico) |
| `UIServices.IsSelectObjectDialogAvailable` | Precisa verificar — pode ser U14+ |
| `UIServices.SelectObjectDialog.SelectObject(options)` | Precisa verificar — pode ser U14+ |
| `BlobPart.FileName` (External File Name) | Pode ser adição mais recente — precisa verificar |
| `DataType.ParseInto(model, dataType, variable)` | Precisa verificar |

#### 2.5 `UIServices.IsKBAvailable` e `UIServices.KB.CurrentKB`

Essas APIs são do `Artech.Architecture.UI.Framework` e são muito básicas — provavelmente disponíveis desde GX17. Mas precisam de confirmação.

---

### Camada 3: Mecanismo de Instalação/Registro (IMPACTO ALTO)

| Item | GX 18 U14+ | GX 18 U13- |
|------|-----------|------------|
| Pasta de destino da DLL | `GeneXus18\Packages\` | `GeneXus18\Packages\` (mesma) |
| Registro | `genexus.exe /install` | `genexus.exe /install` (mesmo) |
| Arquivo `.package` (manifesto) | Idêntico | Idêntico |
| Script `Copy-ExtensionForGeneXus18.ps1` | Hardcoded para `GeneXus18` | Funciona — independe da versão |

**Conclusão:** Os scripts de instalação/registro **funcionam igualmente** no U13-. O mecanismo de deploy é o mesmo desde GX18.

---

### Camada 4: Objeto `API` no GeneXus — Linha do tempo de recursos

| Recurso | Versão GeneXus | Impacto no GOAB |
|---------|---------------|-----------------|
| Objeto API (existência básica) | **GX 17 U1** (dez/2020) | Limite inferior absoluto do projeto |
| `[SecurityLevel]` no ServiceSource | **GX 17 U1** | Necessário para a extensão |
| `&RestCode` standard variable | **GX 17 U1** | Usado nos Events do API Object |
| `Events.Source` part | **GX 17 U1** | Usado para injetar `Event Get.After` etc. |
| `[RestMethod(POST)]` annotation | **GX 17 U1** | Necessário para Create/Update |
| `Required` property em variável de serviço (`idVarServiceRequired`) | **Incerto** — possivelmente GX 18 U8+ | Degrada graciosamente se ausente |
| `SelectObjectDialog` público | **Incerto** — possivelmente GX 18 U10+ | Necessário para seleção de Transaction no wizard |

---

## Fronteiras Realistas de Versão

### Limite Inferior Absoluto: GeneXus 17 U1

O Objeto API foi criado no GeneXus 17 U1 — não há razão técnica funcional para suportar versão anterior. O GOAB explicitamente depende do API Object para sua função central.

### Limite Inferior Prático: A Definir (GX 17 U1 ou GX 18 Ux)

A definição exata depende de:
1. **Disponibilidade do feed NuGet** para versões antigas (chave de build)
2. **Disponibilidade da `SelectObjectDialog` API** (chave de UX)
3. **Compatibilidade binária** dos assemblies — extensões GeneXus são compiladas contra uma versão específica e verificadas na carga

> [!IMPORTANT]
> A GeneXus verifica compatibilidade binária na carga da extensão. Uma DLL compilada com `18.13.2` **não carregará** no U13 (que usa `18.12.x`). É necessário **recompilar** para cada faixa de versão suportada.

---

## O Que Precisa Ser Feito — Itens Técnicos

### Grupo A: Build Multi-versão (FUNDAMENTAL)

1. **Pesquisar feed NuGet** (`pkgs.dev.azure.com/genexuslabs/.../genexus-build-sdk`) para confirmar quais versões de `Artech.*.Sdk` estão publicadas (ex: `18.12.x` para U13, `18.11.x` para U12, etc.)

2. **Criar variantes de `Directory.Build.props`** por faixa de versão:
   - `Directory.Build.props.GX18U14plus` → `18.13.x` (atual)
   - `Directory.Build.props.GX18U13` → `18.12.x`
   - `Directory.Build.props.GX18U12` → `18.11.x`
   - `Directory.Build.props.GX17Ux` → pacotes `17.x.y` (se existirem no NuGet) ou instalador legado

3. **Criar pipelines de build separados** ou parâmetros de build para compilar a DLL targeting cada versão:
   ```
   GenexusOpenApiBuilder.Extension-GX18U14plus.dll
   GenexusOpenApiBuilder.Extension-GX18U13.dll
   GenexusOpenApiBuilder.Extension-GX17.dll
   ```

4. **Atualizar `packages.lock.json`** — o lock file atual está fixo em `18.13.2`. Cada variante precisa de seu próprio lock file.

5. **Para GX17 (se NuGet não tiver pacotes GX17):** Avaliar o uso do SDK instalador legado com referências diretas às DLLs locais — provavelmente via `<HintPath>` no `.csproj`.

---

### Grupo B: Isolamento de APIs por Versão (MÉDIO)

6. **Auditar `UIServices.IsSelectObjectDialogAvailable` e `UIServices.SelectObjectDialog`**: Se essa API não existir em U13-, o wizard de seleção de Transaction precisará de fallback — por exemplo, uma lista de nomes em ComboBox em vez do diálogo nativo.

7. **Auditar `BlobPart.FileName`** (usado para salvar o metadata File com nome externo): Se não disponível em versões antigas, adaptar para usar apenas `BlobPart.Data`.

8. **Auditar `DataType.ParseInto(model, dataType, variable)`**: Se a assinatura mudou entre versões, precisará de wrapper.

9. **Verificar `ContainsPropertyDefinition`** — o código já faz `variable.ContainsPropertyDefinition(ServiceRequiredPropertyId)` antes de setar, o que é defensivo. Confirmar que o método existe em todas as versões.

10. **Verificar `api.Events.Source`** — se o part `Events` não existir em versões antigas do SDK (ex: GX17), o writer de `CreateB079ApiEvents()` falhará ao tentar acessar `api.Events.Source`. Adicionar verificação de nulidade ou try/catch defensivo.

---

### Grupo C: Instalação e Distribuição Multi-versão (OPERACIONAL)

11. **Criar scripts de instalação separados** por versão:
    - `Install-ExtensionForGeneXus18U14plus.bat`
    - `Install-ExtensionForGeneXus18U13.bat`
    - `Install-ExtensionForGeneXus17.bat`
    
    Cada script apontará para o GeneXus Directory correto e para a DLL compilada para aquela versão.

12. **Atualizar `Copy-ExtensionForGeneXus18.ps1`** para receber o diretório GeneXus como parâmetro (já tem `$GeneXusDirectory` — apenas tornar mais visível na documentação).

13. **Documentar** claramente quais DLLs correspondem a quais versões.

---

### Grupo D: Testes por Versão (VALIDAÇÃO)

14. **Criar matriz de validação** por versão (GX17U1, GX17U8, GX18U10, GX18U12, GX18U13, GX18U14, GX18U15), cobrindo:
    - Extensão carrega
    - Menu aparece
    - Wizard abre
    - API Object é criado
    - Procedures e SDTs são criados
    - Metadata File é salvo e relido

15. **Adaptar scripts PowerShell de testes** existentes em `Tests/` para aceitar parâmetro de ambiente (diretório GeneXus + versão).

---

## Mapeamento de Riscos por Funcionalidade

| Funcionalidade | Risco no U13- | Mitigação |
|---------------|--------------|-----------|
| Criar API Object | **Baixo** — API estável desde GX17U1 | Testar carga da DLL |
| `[SecurityLevel]` no source | **Baixo** — é string, não API | GeneXus pode não processar em versões antigas |
| `Events.Source` do API Object | **Médio** — pode não existir em GX17 | Try/catch + fallback sem Events |
| `idVarServiceRequired` | **Baixo** — código já é defensivo | Já possui `ContainsPropertyDefinition` guard |
| `SelectObjectDialog` | **Alto** — pode ser API nova | ComboBox de fallback |
| `BlobPart.FileName` | **Médio** — pode ser propriedade recente | Usar apenas `BlobPart.Data` |
| Metadata File (JSON) | **Baixo** — `WikiFileKBObject` é tipo clássico | Testar persistência |
| Build (NuGet) | **Crítico** — bloqueante para U13- | Verificar feed / SDK legado |

---

## Estratégia de Versões Suportadas Recomendada

Com base na análise:

```
GeneXus 17 U1  → Possível (limite funcional do API Object), mas BUILD é obstáculo crítico
                  Requer SDK legado (instalador) ou NuGet GX17 — precisa ser verificado
                  Risco alto: APIs como Events.Source podem não existir

GeneXus 18 U1  → Possível (mesmo modelo SDK legado), com adaptações moderadas
a GX18 U13       Sem NuGet: requer SDK instalador para compilar
                  APIs funcionais devem ser estáveis

GeneXus 18 U14 → Atual (NuGet nativo, U15 validado)
a GX18 U15       Funcional sem alterações
```

### Recomendação de versão mínima realista

**GeneXus 18 U1** como versão mínima suportada é razoável, pois:
- Mesmo modelo de instalação de extensões (pasta `Packages` + `/install`)
- Objeto API disponível (desde GX17U1)
- APIs SDK provavelmente estáveis desde GX18U1
- O maior obstáculo (build) é vencido se a GeneXus tiver pacotes NuGet para U1 no feed (a verificar)

**GeneXus 17 U1** como versão mínima é tecnicamente possível, mas com risco maior:
- Incerteza sobre APIs de Events, Variables e UIServices
- Provavelmente requer SDK instalador legado para compilar
- Requer testes extensivos

---

## Questões em Aberto (para o usuário responder)

> [!IMPORTANT]
> **Q1 — Qual ambiente você tem disponível para testes?**  
> Quais versões do GeneXus você tem instaladas para validar? (GX17, GX18 U1-U13?)
> Isso é determinante para saber quais versões valem o investimento.

> [!IMPORTANT]
> **Q2 — Qual é o limite mínimo desejado?**
> - Apenas GX 18 U13? (versão imediatamente anterior ao U14)
> - GX 18 U1 em diante?
> - GX 17 U1 em diante?

> [!IMPORTANT]
> **Q3 — Verificar feed NuGet:**  
> Antes de qualquer implementação, verificar no feed `pkgs.dev.azure.com/genexuslabs/.../genexus-build-sdk` quais versões de `Artech.Architecture.Common.Sdk` estão disponíveis.  
> Se apenas `18.13.x` estiver publicado, o modelo NuGet **não funcionará** para U13.

> [!NOTE]
> **Q4 — Funcionalidade `SelectObjectDialog`:**  
> O wizard usa `UIServices.IsSelectObjectDialogAvailable` + `UIServices.SelectObjectDialog.SelectObject()` para deixar o usuário escolher a Transaction. Se essa API não existir no U13-, o wizard ainda funcionará pelo **menu de contexto da Transaction** (clique direito → Genexus Open API Builder → Wizard). Isso pode ser suficiente como fallback.

---

## Mudanças de Código por Arquivo

### [MODIFY] [Directory.Build.props](file:///C:/Projetos/Genexus-Open-API-Builder/Directory.Build.props)
- Parametrizar `GeneXusPackageReferenceVersion` para aceitar múltiplas versões
- Criar variantes por versão GeneXus alvo

### [MODIFY] [global.json](file:///C:/Projetos/Genexus-Open-API-Builder/global.json)
- Criar variante para versões anteriores ao U14 (SDK legado ou NuGet de versão mais antiga)

### [MODIFY] [GenexusOpenApiBuilder.Extension.csproj](file:///C:/Projetos/Genexus-Open-API-Builder/Src/Extension/GenexusOpenApiBuilder.Extension.csproj)
- Adicionar `<Condition>` ou propriedades de build por versão
- Para GX13-: referenciar DLLs via `<Reference>` com `HintPath` (modelo legado)

### [NEW] `packages.lock.GX18U13.json`
- Lock file específico para versão GX18U13

### [MODIFY] [ApiPlanBusinessComponentWriter.cs](file:///C:/Projetos/Genexus-Open-API-Builder/Src/Extension/Diagnostics/ApiPlanBusinessComponentWriter.cs)
- Adicionar guarda `try/catch` ou verificação de nulidade em `api.Events.Source` para versões que possam não ter o part Events
- Verificar `api.Events` antes de acessar `.Source`

### [MODIFY] [ApiPlanListProcedureWriter.cs](file:///C:/Projetos/Genexus-Open-API-Builder/Src/Extension/Diagnostics/ApiPlanListProcedureWriter.cs)
- Mesmo tratamento defensivo de `api.Events.Source`

### [MODIFY] [Package.cs](file:///C:/Projetos/Genexus-Open-API-Builder/Src/Extension/Package.cs)
- Adicionar verificação de `UIServices.IsSelectObjectDialogAvailable` com fallback (se necessário para U13-)

### [NEW] Scripts de instalação por versão
- `Install-ExtensionForGeneXus18U13.bat`
- `Register-ExtensionForGeneXus18U13.bat`

### [MODIFY] [Copy-ExtensionForGeneXus18.ps1](file:///C:/Projetos/Genexus-Open-API-Builder/Tools/Copy-ExtensionForGeneXus18.ps1)
- Tornar `$GeneXusDirectory` mais proeminente como parâmetro obrigatório documentado

---

## Plano de Verificação

### Antes de qualquer código
1. Consultar o feed NuGet público da GeneXus e listar versões disponíveis de `Artech.Architecture.Common.Sdk`
2. Inspecionar (com ILSpy ou dotPeek) as DLLs do SDK GX18U13 para confirmar existência de `api.Events`, `UIServices.IsSelectObjectDialogAvailable`, `BlobPart.FileName`

### Após build para U13
1. Copiar DLL para `GeneXus18U13\Packages\`
2. Executar `genexus.exe /install` no U13
3. Abrir KB, verificar se menu aparece
4. Abrir Wizard → selecionar Transaction → confirmar geração básica
5. Confirmar criação de API Object, SDTs, Procedures e metadata File

### Teste de regressão no U14+
- Após qualquer mudança de código (guards defensivos), revalidar no U15 para garantir que nenhum comportamento existente foi quebrado

---

## Resumo Executivo

| | Status |
|-|--------|
| **Funciona no U14+ (NuGet)** | ✅ Sim — validado no U15 |
| **Funciona no U13 sem mudanças** | ❌ Não — precisa recompilação com SDK U13 |
| **Funciona no U13 com mudanças** | ⚠️ Provavelmente sim, após build multi-versão |
| **Funciona no U1-U12 com mudanças** | ⚠️ Dependente de APIs disponíveis — requer auditoria |
| **Funciona no GX17U1+** | ⚠️ Tecnicamente possível, mas requer investigação aprofundada e testes |
| **Funciona antes do GX17U1** | ❌ Não — o Objeto API não existia |

O **maior bloqueio é de build** (modelo NuGet vs SDK legado), não de APIs funcionais. A maioria das APIs usadas são estáveis desde GX17U1. O plano de implementação é viável e os riscos são identificados e gerenciáveis.
