# B000 — Carregamento da Extensão na IDE

## Estado

Bloqueado em 2026-07-15 antes de qualquer tentativa de implantação na IDE.

## Objetivo

Comprovar que a extensão mínima carrega no GeneXus 18 Upgrade 15 sem criar ou alterar objetos em uma Knowledge Base.

## Evidência coletada

- a instalação localizada é GeneXus 18 Upgrade 15, build `18.0.15.188745`;
- há assemblies `Artech.Architecture.*` suficientes para compilar a biblioteca mínima;
- não há instalação separada do GeneXus Platform SDK no registro do Windows;
- não foram encontrados diretórios dedicados `PackageBuilder`, `Samples` ou SDK na raiz da instalação consultada;
- a documentação oficial do Platform SDK estabelece que extensões clássicas são disponibilizadas ao copiar a DLL para a pasta `Packages` do GeneXus e iniciar a IDE uma vez com `/install`.

Fonte oficial: [GeneXus Platform SDK](https://wiki.genexus.com/commwiki/wiki?3271,GeneXus+Platform+SDK).

## Bloqueio

A regra local em `AGENTS.md` proíbe criar, alterar, mover, renomear ou excluir itens em `C:\Program Files (x86)\GeneXus` e subpastas. Portanto, não foi copiada DLL para `Packages`, não foi executado `genexus.exe /install` e a IDE não foi iniciada para registrar a extensão.

Os assemblies instalados permitem validar a compilação realizada em B010, mas não substituem os artefatos, o assistente e o fluxo de implantação do Platform SDK.

## Sem efeitos colaterais

- nenhuma pasta ou arquivo da instalação do GeneXus foi alterado;
- nenhuma Knowledge Base foi aberta ou modificada;
- nenhum registro de extensão foi criado.

## Condição para retomar

É necessário um ambiente de teste explicitamente autorizado, fora da instalação protegida, contendo GeneXus 18 Upgrade 15 e o Platform SDK compatível, ou autorização explícita e limitada para registrar a extensão em uma instalação de teste. A partir dele, B000 poderá criar o pacote mínimo, implantar a DLL, executar `/install` e comprovar o carregamento.