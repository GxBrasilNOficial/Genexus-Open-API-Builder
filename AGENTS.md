# Instruções locais para agentes

## Proteção da instalação do GeneXus

- É proibido alterar, criar, mover, renomear ou excluir qualquer arquivo ou pasta em `C:\Program Files (x86)\GeneXus` ou em suas subpastas.
- Essa instalação pode ser consultada somente em modo leitura para localizar e inspecionar o Extensibility SDK e suas dependências.
- Artefatos do projeto devem ser criados apenas dentro deste repositório.

## Higiene de documentação Markdown

- Após qualquer alteração em arquivos `.md`, validar que o arquivo termina com LF final, especialmente porque `.gitattributes` define `*.md text eol=lf`.
- Em PowerShell, uma verificação direta é `[IO.File]::ReadAllBytes($path)[-1] -eq 10`; não considerar a edição concluída enquanto o último byte não for `10`.
- `git diff --check` não acusa ausência de newline final, então esta conferência deve ser explícita antes de commitar documentação.

## Atualização manual da extensão para testes

Sempre que uma nova DLL precisar ser instalada para teste no GeneXus 18, o agente deve primeiro distinguir atualização de código de atualização de manifesto/registro.

Para atualização apenas de DLL, sem alteração em `Src/Extension/GenexusOpenApiBuilder.package`, na identidade do pacote ou no registro da extensão:

1. fechar completamente a IDE GeneXus;
2. executar `Install-ExtensionForGeneXus18.bat`, na raiz do repositório, usando **Executar como administrador**;
3. abrir novamente a IDE e executar a validação funcional indicada para a frente.

O instalador já executa `Tools/Test-InstalledExtension.ps1` ao final e falha quando a DLL instalada não corresponde à build atual.

Quando houver, desde o ultimo `genexus /install` bem-sucedido, alteracao em `Src/Extension/GenexusOpenApiBuilder.package`, na identidade do pacote ou no registro da extensao, acrescentar entre os passos 2 e 3:

1. executar `Register-ExtensionForGeneXus18.bat` normalmente, sem Administrador;
2. no prompt aberto pelo segundo arquivo, digitar `genexus /install`, conferir a varredura e depois digitar `exit`.

- `Install-ExtensionForGeneXus18.bat` é sempre o caminho operacional primário para instalar a nova DLL; `Register-ExtensionForGeneXus18.bat` é condicional à atualização de manifesto/registro.
- O agente não executa esses arquivos nem altera `C:\Program Files (x86)\GeneXus`; apenas orienta a execução manual.
- Não substituir a instalação por uma chamada direta a `Tools/Copy-ExtensionForGeneXus18.ps1`. O `.ps1` é implementação interna exclusiva da etapa de cópia e validação; ele não registra a extensão.
- Ao avisar que chegou a hora de atualizar e testar, declarar explicitamente se o manifesto/registro mudou e solicitar `genexus /install` somente nesse caso.

## Registro de comandos no menu de contexto

Cada inclusão, alteração ou remoção de comando do menu de contexto deve manter sincronizadas, no mesmo passo, estas três camadas:

1. registro em runtime por `AddCommand(new CommandKey(...))` em `Src/Extension/Package.cs`;
2. `CommandDefinition` em `Src/Extension/GenexusOpenApiBuilder.package`;
3. `Command refid` dentro do grupo de comandos em `Groups` no mesmo manifesto, grupo que o submenu referencia.

- O ID deve ser exatamente igual nas três camadas.
- Para comandos de menu, registrar o ID em `Package.cs` como string literal no `CommandKey`, no formato `new CommandKey(Id, "Nome do Comando")`. O checker `Tools/Test-ExtensionCommandRegistration.ps1` valida esse contrato por leitura textual e não resolve constantes ou campos intermediários.
- O build bem-sucedido não comprova essa sincronização.
- Antes de gerar uma DLL para atualização manual, executar:

```powershell
pwsh -NoProfile -File Tools/Test-ExtensionCommandRegistration.ps1
```

- Preservar `Futura Primeira Opção` como placeholder não operacional; comandos temporários devem ser acrescentados sem substituir o placeholder.
- No fechamento de uma sonda, remover seus comandos das três camadas e executar novamente o teste.

## Escrita na janela Output da IDE

Para mensagens da extensão na janela Output do GeneXus, reutilizar o padrão validado no B001/B020:

1. conferir `CommonServices.IsOutputAvailable`;
2. obter `CommonServices.Output`;
3. exigir `IOutputService2` e usar `DefaultOutputId`;
4. escrever com `AddLine(outputId, mensagem)`;
5. chamar `Show(outputId)` após escrever.

Não escrever em um Output customizado sem criação/seleção comprovada na IDE. O primeiro teste manual de B020 mostrou que o comando podia aparecer e executar sem mensagem visível quando a implementação tentava usar um `outputId` customizado. O Output padrão da IDE foi o caminho confirmado no U15.

## Promoção de frente e próximo passo

Ao concluir uma frente e promover a próxima ação, não atualizar apenas o checkpoint operacional. Antes de commitar, buscar no repositório inteiro pelo ID concluído, pelo ID seguinte e por expressões como `próxima frente`, `próxima missão`, `próxima ação`, `próxima responsabilidade operacional`, nomes de comandos adicionados/removidos e nomes de classes de sonda.

Quando a frente alterar o comportamento em runtime da extensão, revisar também comentários XML/C#, descrições de classe/método e termos de transição como `passivo`, `placeholder`, `temporário`, `sonda`, `runtime`, `manual`, `somente leitura`, `comando` e `protótipo`. Comentários e documentos devem declarar se o comando ou comportamento permanece, foi removido ou será absorvido por fluxo futuro.

Para cada ocorrência encontrada:

- atualizar quando ela afirmar um próximo passo que deixou de ser vigente;
- manter quando for range, histórico da própria frente ou referência governante ainda correta;
- registrar mentalmente a justificativa para flags descartadas, para reportar na revisão pré-push quando aplicável.

O checkpoint `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md` continua sendo a fonte canônica do próximo passo, mas documentos antigos não devem contradizê-lo com frases operacionais obsoletas.

## Revisão pré-push do repositório

Antes de qualquer push:

**Aviso obrigatório:** a rotina pré-push deste repositório deve ser executada somente depois de a frente estar commitada. Não tratar execução sobre working tree suja como rotina pré-push válida; nesse caso, no máximo é diagnóstico intermediário. Primeiro criar o commit local da frente e só então executar os passos abaixo para revisar o intervalo commitado contra `origin/main`.

1. executar `git fetch origin` separadamente para atualizar `origin/main`;
2. na raiz do repositório, executar exatamente:

```powershell
pwsh -NoProfile -File scripts/Invoke-PrePushMechanicalChecks.ps1 -AsJson
```

3. ler `pushReadiness`, `incompleteReasons`, `manualRequired` e `notCovered` no JSON;
4. na resposta final da rotina, terminar sempre com uma frase explícita: `Sem impedimento para push.` quando `pushReadiness` estiver pronto, `behind=0`, working tree limpa, `manualRequired=[]`, `incompleteReasons=[]` e a revisão semântica não tiver gap bloqueante; caso contrário, terminar com `Com impedimento para push:` seguido do motivo objetivo;
5. concluir a revisão semântica exigida pelas instruções globais; `exit 0` mecânico não substitui essa revisão;
6. quando o checker ou seu teste mudar, executar também `pwsh -NoProfile -File Tests/PrePushChecker/Test-OpenApiBuilderPrePushChecks.ps1`.

- `scripts/Invoke-PrePushMechanicalChecks.ps1` é o nome canônico e não deve divergir do contrato global.
- `manualRequired` bloqueia o push até a revisão humana registrar gaps confirmados, flags descartados e áreas não cobertas.

### Revisão semântica de contrato runtime

Além de buscar termos e inconsistências documentais, a revisão semântica pré-push deve reconstruir o fluxo runtime afetado pelo commit.

Para cada mudança que altere assinatura, `Source`, `Rules`, variáveis, parâmetros, chamada entre objetos, geração de artefatos ou estado persistido:

1. Identificar todos os produtores e consumidores do contrato alterado. Exemplos:
   - `Procedure.Rules.Source` com `parm(...)` alterado exige revisar todos os callers gerados;
   - `API.ServiceGroupSource.Source` alterado exige revisar variáveis, parâmetros e Procedures chamadas;
   - SDT alterado exige revisar variáveis, payloads, API Object e Procedures que usam o SDT.
2. Conferir se cada consumidor foi atualizado no mesmo commit. Build Release da extensão não valida semanticamente o código GeneXus gerado.
3. Conferir se o preflight cobre todos os objetos que serão gravados antes do primeiro `Save()`. Quando o fluxo grava mais de um objeto, procurar risco de escrita parcial em resolução de tipos, criação/remoção de variáveis, `Source`, `Rules`, objetos ausentes, colisões externas e validação pós-save.
4. Conferir se a evidência manual cobre o consumidor final do contrato. Se uma Procedure é chamada por API Object, validar também especificação/build do API Object, não só da Procedure.
5. Declarar no relatório da revisão:
   - contratos alterados;
   - produtores;
   - consumidores;
   - evidência de atualização de cada consumidor;
   - risco de escrita parcial encontrado ou descartado;
   - validação manual/runtime ainda faltante.

### GeneXus Open API Builder — trio API/Procedure/SDT

Quando a mudança afetar `API Object`, `Procedure`, `SDT`, `Rules parm`, `Variables` ou `ServiceGroupSource`, a revisão pré-push deve verificar obrigatoriamente o trio:

- assinatura declarada em `Procedure.Rules.Source`;
- chamada gerada em `API.ServiceGroupSource.Source`;
- variáveis declaradas em `API.Variables.Content.Content` e `Procedure.Variables`.

Qualquer divergência entre esses três pontos é gap P1 e bloqueia push.

## Fechamento de spikes e sondas temporárias

Antes de concluir e commitar qualquer item de spike `B000`–`B006`, o agente deve:

- distinguir a evidência histórica do comportamento que deve permanecer no runtime;
- remover eventos, comandos, menus e gatilhos temporários após a validação, salvo decisão explícita e documentada para mantê-los;
- não deixar sondas capazes de ler ou escrever automaticamente em qualquer KB;
- não deixar comandos experimentais de escrita disponíveis fora do escopo autorizado para o teste;
- preservar o código da sonda somente quando ele tiver valor técnico ou documental e garantir, por busca, que o runtime não o invoque;
- recompilar a extensão e solicitar ao usuário a reinstalação manual da DLL passiva, sem o agente alterar `C:\Program Files (x86)\GeneXus`;
- confirmar por teste de leitura que a DLL instalada coincide com a build e que a sonda encerrada não está mais registrada ou ativa;
- atualizar no mesmo fechamento o `CHANGELOG.md`, `Docs/STATUS_ATUAL_E_PROXIMO_PASSO.md`, `Docs/Foundation/24-PLANO_IMPLEMENTACAO_REAL_POR_SPRINTS.md` e os documentos que ainda indiquem a frente encerrada como próxima;
- buscar no repositório inteiro o ID encerrado, o ID seguinte, os nomes dos comandos e os nomes das classes de sonda para localizar referências operacionais contraditórias;
- só considerar o marco pronto para revisão pré-push depois dessas validações.
