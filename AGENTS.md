# Instruções locais para agentes

## Proteção da instalação do GeneXus

- É proibido alterar, criar, mover, renomear ou excluir qualquer arquivo ou pasta em `C:\Program Files (x86)\GeneXus` ou em suas subpastas.
- Essa instalação pode ser consultada somente em modo leitura para localizar e inspecionar o Extensibility SDK e suas dependências.
- Artefatos do projeto devem ser criados apenas dentro deste repositório.

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
