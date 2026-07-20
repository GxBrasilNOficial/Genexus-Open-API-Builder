# B025 - Leitura de Chave Primária no Protótipo

Concluído no GeneXus 18 Upgrade 15: a extensão leu a chave primária completa da `Transaction` acionada pelo menu de contexto, em modo somente leitura, preservando ordem e tipos para chave simples e composta.

## Objetivo

Ler, para a `Transaction` selecionada no protótipo navegável, a chave primária simples ou composta completa por API pública e sem persistência.

## Escopo validado

- o comando é acionado por `Genexus Open API Builder > Ler Chave Primária (B025)`, tanto no menu principal quanto no menu de contexto da `Transaction`;
- quando acionado pelo menu de contexto, o comando tenta resolver diretamente a `Transaction` clicada;
- quando acionado pelo menu principal, o comando usa a seleção mantida em memória por B022 como fallback;
- a leitura usa `transaction.Structure.Root.PrimaryKey` e os metadados públicos do atributo associado;
- nenhuma escolha é persistida;
- nenhum objeto é criado, alterado ou excluído pela extensão.

## Implementação

`Src/Extension/Package.cs` registra o comando B025, resolve a `Transaction` do contexto do comando quando disponível, mantém o fallback para a seleção em memória e escreve o resultado na Output padrão.

`Src/Extension/Diagnostics/PrototypePrimaryKeyReader.cs` encapsula a leitura somente leitura da chave primária e produz:

- nome da `Transaction`;
- quantidade de partes da chave;
- indicador de chave composta;
- ordem, nome, tipo, tamanho e casas decimais de cada parte.

## Evidência manual no U15

Validadas duas KBs:

- KB de teste `wsEducacaoSpTeste`, com `Transaction` `Carga` e chave simples;
- KB principal usada somente para leitura, com `Transaction` `AbateOrdem` e chave composta.

Saída observada para chave simples:

```text
[Genexus Open API Builder][B025] Transaction selecionada: Name='Carga', PrimaryKeyParts=1, HasCompositeKey=False.
[Genexus Open API Builder][B025] KeyPart: Order=1, Name='CargaId', Type='NUMERIC', Length=10, Decimals=0.
```

Saída observada para chave composta:

```text
[Genexus Open API Builder][B025] Transaction selecionada: Name='AbateOrdem', PrimaryKeyParts=2, HasCompositeKey=True.
[Genexus Open API Builder][B025] KeyPart: Order=1, Name='AbateOrdemEmpresaId', Type='NUMERIC', Length=10, Decimals=0.
[Genexus Open API Builder][B025] KeyPart: Order=2, Name='AbateOrdemId', Type='NUMERIC', Length=10, Decimals=0.
```

## Resultado

Critério atendido em 2026-07-20: a chave primária simples e composta foi lida por API pública para a `Transaction` selecionada, com nome, quantidade de partes, ordem e tipos apresentados sem persistência nem escrita pela extensão na KB. B030 foi concluído posteriormente; a próxima ação vigente fica no checkpoint operacional.
