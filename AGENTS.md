# Instruções locais para agentes

## Proteção da instalação do GeneXus

- É proibido alterar, criar, mover, renomear ou excluir qualquer arquivo ou pasta em `C:\Program Files (x86)\GeneXus` ou em suas subpastas.
- Essa instalação pode ser consultada somente em modo leitura para localizar e inspecionar o Extensibility SDK e suas dependências.
- Artefatos do projeto devem ser criados apenas dentro deste repositório.

## Atualização manual da extensão para testes

Sempre que uma nova DLL precisar ser instalada para teste no GeneXus 18, o agente deve apresentar ao usuário o fluxo operacional completo abaixo, nesta ordem:

1. fechar completamente a IDE GeneXus;
2. executar `Install-ExtensionForGeneXus18.bat`, na raiz do repositório, usando **Executar como administrador**;
3. executar `Register-ExtensionForGeneXus18.bat` normalmente, sem Administrador;
4. no prompt aberto pelo segundo arquivo, digitar `genexus /install`, conferir a varredura e depois digitar `exit`;
5. abrir novamente a IDE e executar a validação funcional indicada para a frente;
6. quando solicitado, executar `pwsh -NoProfile -File Tools/Test-InstalledExtension.ps1` para conferir por leitura se a DLL instalada coincide com a build.

- Os dois arquivos `.bat` da raiz são o caminho operacional primário para o usuário.
- O agente não executa esses arquivos nem altera `C:\Program Files (x86)\GeneXus`; apenas orienta a execução manual.
- Não substituir esse fluxo por uma chamada direta a `Tools/Copy-ExtensionForGeneXus18.ps1`. O `.ps1` é implementação interna exclusiva da etapa de cópia e validação; ele não registra a extensão.
- Ao avisar que chegou a hora de atualizar e testar, citar sempre os dois `.bat`, suas exigências de elevação distintas e a sequência `genexus /install` seguida de `exit`.

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
