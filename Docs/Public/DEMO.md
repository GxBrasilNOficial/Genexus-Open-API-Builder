# Demo rápida — Genexus Open API Builder

Roteiro visual da Alpha `0.1.0-alpha.1` (Transaction de exemplo: `NotaFiscal`).

Use sempre uma **KB de teste**, com backup. Não execute na KB de produção.

## O que você verá

1. Entrada pelo menu da IDE
2. Percurso completo do Wizard
3. Folder `<Transaction>OpenApi` com os objetos gerados
4. Relatório final após aplicar

---

## 1. Abrir a KB de teste

Abra no GeneXus 18 uma KB pequena, fora de produção.

## 2. Menu principal

Menu **Genexus Open API Builder** (antes de **Help**): Preferências, Wizard, Sincronizar e Remover.

![Menu Genexus Open API Builder](../Images/alpha-menu.png)

## 3. Preferências do Wizard

Defaults por KB (etapas de geração, serviços, segurança e paginação), gravados no File `GxOpenApiBuilder_Settings`.

![Preferências do Wizard](../Images/alpha-preferences.png)

## 4. Menu de contexto da Transaction

No objeto Transaction: **Genexus Open API Builder** → Wizard / Sincronizar / Remover.

![Menu de contexto](../Images/alpha-context-menu.png)

## 5. Wizard — Serviços

Escolha `List`, `Get`, `Create` e `Update` (todos habilitados por padrão no MVP).

![Serviços](../Images/alpha-wizard-servicos.png)

## 6. Requests

Campos de CreateRequest e UpdateRequest. Chave autonumerada fica bloqueada no Create; PK no path fica bloqueada no Update.

![Requests](../Images/alpha-wizard-requests.png)

## 7. Response

Campos devolvidos no response principal.

![Response](../Images/alpha-wizard-response.png)

## 8. Filtros List

Filtros candidatos do serviço List (operadores por tipo).

![Filtros List](../Images/alpha-wizard-filtros.png)

## 9. Paths

`ApiName`, base path, `RestPath` e rotas geradas.

![Paths](../Images/alpha-wizard-paths.png)

## 10. Segurança

`Authentication` (padrão), `Authorization` ou `None` (exige confirmação).

![Segurança](../Images/alpha-wizard-seguranca.png)

## 11. Paginação

Default e máximo de página do List.

![Paginação](../Images/alpha-wizard-paginacao.png)

## 12. Ordenação

Ordenação estática inicial (PK completa como desempate).

![Ordenação](../Images/alpha-wizard-ordenacao.png)

## 13. Obrigatórios

Required no payload de Create (editável) e Update (PUT completo).

![Obrigatórios](../Images/alpha-wizard-obrigatorios.png)

## 14. SDTs

Confirmação para criar/reencontrar estruturas de dados.

![SDTs](../Images/alpha-wizard-sdts.png)

## 15. Procedures

Confirmação das Procedures planejadas.

![Procedures](../Images/alpha-wizard-procedures.png)

## 16. API Object

Confirmação do API Object.

![API Object](../Images/alpha-wizard-api-object.png)

## 17. Business Component

Completar Get/Create/Update REST via Business Component.

![Business Component](../Images/alpha-wizard-business-component.png)

## 18. List

Completar listagem paginada e sincronizar o API Object.

![List](../Images/alpha-wizard-list.png)

## 19. Metadata

File JSON de metadata da API.

![Metadata](../Images/alpha-wizard-metadata.png)

Na KB, o File fica no módulo da Transaction (não dentro do Folder). Properties típicas:

![File apiNotaFiscal_Metadata](../Images/alpha-metadata-file.png)

## 20. Resumo

Decisões acumuladas, endpoints e garantias — depois **Concluir e aplicar**.

![Resumo](../Images/alpha-wizard-resumo.png)

## 21. Relatório final

Após aplicar, o relatório lista criados, atualizados, bloqueados e avisos. No caminho feliz: `Blocked=0` e metadata criada.

![Relatório final](../Images/alpha-relatorio-final.png)

## 22. Objetos gerados

Folder `<Transaction>OpenApi` com API Object, Procedures e SDTs próprios.

![Folder NotaFiscalOpenApi](../Images/alpha-folder.png)

## 23. Build e checagem

Execute Build na API (ou Build All). Se o environment estiver publicado, teste `List`/`Get`/`Create`/`Update` conforme a segurança escolhida.

---

## Comandos úteis depois da primeira geração

| Comando | Uso |
|---------|-----|
| Wizard | Regenerar / complementar de forma conservadora |
| Sincronizar com a Transaction | Diff da estrutura da Transaction vs metadata |
| Remover API gerada | Remoção conservadora dos objetos próprios |

### Sincronizar com a Transaction

Quando a Transaction ganha atributos novos, o Sync mostra o delta e permite marcar onde incluir (Response, Create, Update, Filtros).

![Sincronizar com a Transaction](../Images/alpha-sync.png)

### Remover API gerada

Confirmação com o plano: objetos próprios a apagar, SDTs compartilhados e Folder reutilizado preservados, BC da Transaction intacto.

![Remover API gerada](../Images/alpha-remover.png)

## Limitações honestas da Alpha

- A geração cobre apenas o primeiro nível da Transaction; subníveis (linhas) são ignorados sem aviso
- Sem serviço `DELETE` no MVP
- YAML OpenAPI nativo do GeneXus tem restrições documentadas
- Classificação de campos sensíveis/auditoria ainda usa política default
- Validação prática principal no Upgrade 15

Detalhes: [notas 0.1.0-alpha.1](../Releases/0.1.0-alpha.1.md).

## Índice das capturas

Todas em `Docs/Images/`:

| Arquivo | Cena |
|---------|------|
| `alpha-menu.png` | Menu principal |
| `alpha-preferences.png` | Preferências do Wizard |
| `alpha-context-menu.png` | Menu de contexto |
| `alpha-wizard-servicos.png` | Serviços |
| `alpha-wizard-requests.png` | Requests |
| `alpha-wizard-response.png` | Response |
| `alpha-wizard-filtros.png` | Filtros List |
| `alpha-wizard-paths.png` | Paths |
| `alpha-wizard-seguranca.png` | Segurança |
| `alpha-wizard-paginacao.png` | Paginação |
| `alpha-wizard-ordenacao.png` | Ordenação |
| `alpha-wizard-obrigatorios.png` | Obrigatórios |
| `alpha-wizard-sdts.png` | SDTs |
| `alpha-wizard-procedures.png` | Procedures |
| `alpha-wizard-api-object.png` | API Object |
| `alpha-wizard-business-component.png` | Business Component |
| `alpha-wizard-list.png` | List |
| `alpha-wizard-metadata.png` | Metadata |
| `alpha-metadata-file.png` | Properties do File `api*_Metadata` |
| `alpha-wizard-resumo.png` / `alpha-wizard.png` | Resumo |
| `alpha-relatorio-final.png` | Relatório final |
| `alpha-folder.png` | Folder gerado |
| `alpha-sync.png` | Sincronizar com a Transaction |
| `alpha-remover.png` | Remover API gerada |
