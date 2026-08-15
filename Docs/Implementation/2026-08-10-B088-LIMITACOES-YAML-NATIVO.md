# B088 — Limitações do YAML OpenAPI nativo do GeneXus

## Objetivo

Fechar a frente pré-Alpha `B088`: investigar se a Extensibility IDE/SDK permite substituir ou interceptar o template nativo de documentação REST (`Swagger.Yaml.stg` / `TypeDefinitions.Yaml.stg`) sem alterar a instalação GeneXus; se não, registrar a limitação intransponível e as orientações de consumo.

## Escopo

- Inclui: respostas HTTP declaradas no YAML; emissão de `required:` nos schemas; extensibilidade do gerador REST/YAML; orientação a consumidores humanos, `openapi-generator-cli` e agentes de IA.
- Exclui: evidência HTTP `403` GAM (`B089`); alteração de código da extensão; patch em `C:\Program Files (x86)\GeneXus`.

## Resultado

**Limitação intransponível neste produto.** Não há via suportada de substituir ou interceptar os templates StringTemplate da documentação OpenAPI sem modificar a instalação central. O MVP permanece útil com runtime HTTP rico e YAML nativo pobre na lista de `responses:` e no bloco `required:` dos schemas.

## Sintomas (já evidenciados; reconfirmados)

Fonte histórica: `Docs/Implementation/2026-08-03-CONTRATO-OPENAPI-GAPS.md`.

1. Cada operação de API Object declara tipicamente só `200` e `404` no YAML, enquanto o runtime desta ferramenta devolve também `201`, `400`, `401`, `422`, etc.
2. O bloco `required:` dos schemas não é emitido mesmo com a propriedade `Required` persistida em item de SDT. O que a extensão controla é `requestBody: required: true` via `Required` nas variáveis de request do API Object.

Reconfirmação read-only em 2026-08-10 na instalação GeneXus 18 (U15):

- `Packages\RestDLTemplates\Swagger.Yaml.stg` — template `procedure_responses` literal com `200` e `404`.
- `Packages\RestDLTemplates\TypeDefinitions.Yaml.stg` — `required:` só se `level.RequiredAttributes` não for vazio.
- SHA256 de `Swagger.Yaml.stg` idêntico em GeneXus18, GeneXus18Up14 e GeneXus18Up14HotFix: `307BA312360785A72543DD6BEE2335CB08E9F4ED16A43F790AF96DC5E886D29A`.
- Nenhuma cópia/override de `RestDLTemplates` ou `Swagger.Yaml.stg` encontrada sob `C:\KBs`.

Códigos adicionais no template existem apenas em `bc_responses*` (REST automático de Business Component) e no endpoint `gxobject` — caminhos que não são o API Object gerado por esta ferramenta.

## Investigação de extensibilidade

Gerador: `Packages\Artech.Packages.RestServiceDL.Generator.dll`, tipo `GeneratorService`.

Campos fixos no construtor (refletidos em 2026-08-10):

- `TEMPLATES = RestDLTemplates`
- `TEMPLATE_FORMAT = Swagger.Yaml.stg`
- `PLUGIN_DIR = api_plugins` e `OPENAPI3_DIR = OpenApi3` (metadados de plugin OpenAI / OpenAPI3 auxiliar; não customizam `responses:` nem `required:` do API Object)

IL de `GenerateSwagger` / `GenerateAPIDef`:

- resolve templates via `PathHelper.get_PackagesPath` + `Path.Combine` + `TemplateGroupFile` sob `Packages\RestDLTemplates`;
- não há strings/APIs de override por KB, `UserTemplates`, `CustomTemplates` ou shadow path.

Wiki oficial (*Generate OpenAPI interface property*): apenas liga/desliga a geração do YAML; não documenta substituição de template.

A extensão deste repositório (`Package.cs` / `GenexusOpenApiBuilder.package`) registra comandos e UI; não há hook de gerador REST/YAML.

### Vias descartadas

| Via | Motivo |
| --- | --- |
| Editar `Packages\RestDLTemplates\*.stg` na instalação | Proibido por `AGENTS.md`; frágil em upgrade |
| Extensibility SDK interceptar o template | Ausente no loader inspecionado |
| Extensão reescrever o `.yaml` gerado | Viola a regra de não escrever YAML direto; Build All sobrescreve |
| Marcar `Required` em item de SDT | Persistido, mas ignorado pelo modelo REST para `required:` de schema |
| Usar `Description` / `[Description]` para listar códigos HTTP | Texto livre vai a `info.description` / `summary`; não preenche `responses:`; inadequado como contrato de status |

## Orientação de consumo

1. Tratar a tabela de status do documento 27 e o Source/Events gerados (`&RestStatusCode` nas Procedures; `&RestCode = &RestStatusCode` em `List.After` / `Get.After` / `Create.After` / `Update.After`) como fonte dos códigos HTTP do MVP — não o bloco `responses:` do YAML nativo.
2. `openapi-generator-cli` (evidência Sprint 6 com 5.3.1, `typescript-fetch` e `csharp`) continua útil para rotas, métodos, `operationId`, security e schemas básicos; o cliente gerado pode tratar `201`/`400`/`422` como respostas fora do mapa declarado.
3. Agentes de IA que leiam só o YAML devem ser avisados dessa limitação e, para completar o quadro de status, cruzar Source das Procedures / Events do API Object (ou o C# gerado pós-Build) e o contrato HTTP do projeto. Códigos do GAM/pipeline (`401`/`403`, falhas de infra) podem não aparecer no Source da Procedure.
4. Não usar `Description` do API Object nem descriptions de serviço como substituto estruturado da lista de status.

## Critérios de aceite B088

1. Extensibilidade mapeada e inviabilidade comprovada sem tocar na instalação — atendido neste relatório.
2. Ressalva e orientação de consumo nos documentos 12 e 27 — atendido no mesmo fechamento.
3. Relatório técnico de viabilidade entregue — este documento.

## Relacionados

- `Docs/Implementation/2026-08-03-CONTRATO-OPENAPI-GAPS.md`
- `Docs/Implementation/2026-08-04-VALIDACAO-YAML-SPRINT6-EIXOS-SEGURANCA.md`
- `Docs/Foundation/06-BACKLOG_v0.1.md` (nota operacional B088)
- `Docs/Foundation/12-REGRAS_CRIACAO_API_OBJECTS.md`
- `Docs/Foundation/27-CONTRATO_HTTP_ERROS_E_SDTS_COMPARTILHADOS.md`
