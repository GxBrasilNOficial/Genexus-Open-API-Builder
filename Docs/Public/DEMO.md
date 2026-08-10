# Demo rápida — Genexus Open API Builder

Roteiro de **3 a 8 minutos** para ver valor na Alpha `0.1.0-alpha.1`.

Use sempre uma **KB de teste**, com backup. Não execute na KB de produção.

## O que você verá

1. Transaction original  
2. Wizard gerando a API  
3. Objetos criados no Folder `<Transaction>OpenApi`  
4. Código editável (Procedures / API Object)  
5. Ganho de tempo frente à montagem manual  

## Passo a passo

### 1. Abrir a KB de teste

Abra no GeneXus 18 uma KB pequena, fora de produção.

### 2. (Opcional) Preferências do Wizard

Menu **Genexus Open API Builder** → **Configurar Preferências do Wizard**.

Ajuste defaults da KB (serviços, segurança, paginação, etapas de geração) e grave. O File `GxOpenApiBuilder_Settings` fica na KB.

### 3. Abrir o Wizard

Em uma Transaction adequada (com Business Component habilitado, ou permita que o Wizard o habilite com confirmação):

- menu principal **Wizard**, ou  
- menu de contexto da Transaction → **Genexus Open API Builder** → **Wizard**

Percorra as abas (serviços, requests, response, filtros, paths/segurança, obrigatórios, geração) e conclua com aplicação quando o resumo indicar etapas a gravar.

### 4. Conferir os objetos gerados

No módulo da Transaction, abra o Folder `<NomeDaTransaction>OpenApi` e confira:

- API Object `api<Nome>`
- Procedures `proc<Nome>_API_List|Get|Create|Update`
- SDTs próprios `sdt<Nome>_API_*`
- File de metadata `api<Nome>_Metadata`

SDTs compartilhados de erro e paginação ficam em `GxOpenAPI`.

### 5. Build e checagem

Execute Build na API (ou Build All do environment). Em seguida, se o ambiente estiver publicado, teste `List`/`Get`/`Create`/`Update` conforme a segurança escolhida (None / Authentication / Authorization + GAM).

## Comandos úteis depois da primeira geração

| Comando | Uso |
|---------|-----|
| Wizard | Regenerar / complementar de forma conservadora |
| Sincronizar com a Transaction | Diff da estrutura da Transaction vs metadata |
| Remover API gerada | Remoção conservadora dos objetos próprios |

## Limitações honestas da Alpha

- Sem serviço `DELETE` no MVP  
- YAML OpenAPI nativo do GeneXus tem restrições documentadas (respostas declaradas, `required` em schemas)  
- Classificação de campos sensíveis/auditoria ainda usa política default (não metadata canônica por KB)  
- Validação prática principal no Upgrade 15  

Detalhes: [notas 0.1.0-alpha.1](../Releases/0.1.0-alpha.1.md).

## Capturas

Arquivos esperados em `Docs/Images/`:

- `alpha-menu.png`
- `alpha-wizard.png`
- `alpha-folder.png`
- `alpha-preferences.png`

Se alguma imagem ainda não estiver no repositório, o roteiro textual acima continua válido.
