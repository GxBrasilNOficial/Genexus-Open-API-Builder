using System;
using System.Collections.Generic;
using System.Globalization;
using Artech.Architecture.Common.Objects;
using Artech.Architecture.UI.Framework.Services;
using Artech.Genexus.Common.Objects;
using GenexusOpenApiBuilder.Extension.Domain;

namespace GenexusOpenApiBuilder.Extension;

internal sealed class ExtensionTexts
{
    public ExtensionTexts(ExtensionLanguage language)
    {
        Language = language;
    }

    public ExtensionLanguage Language { get; }

    public string ConfigureWizardPreferences => Language switch
    {
        ExtensionLanguage.PortugueseBrazil => "Configurar Preferências do Wizard",
        ExtensionLanguage.Spanish => "Configurar preferencias del Wizard",
        _ => "Configure Wizard Preferences",
    };

    public string Wizard => "Wizard";

    public string SynchronizeWithTransaction => Language switch
    {
        ExtensionLanguage.PortugueseBrazil => "Sincronizar com a Transaction",
        ExtensionLanguage.Spanish => "Sincronizar con la Transaction",
        _ => "Synchronize with the Transaction",
    };

    public string RemoveGeneratedApi => Language switch
    {
        ExtensionLanguage.PortugueseBrazil => "Remover API gerada",
        ExtensionLanguage.Spanish => "Eliminar API generada",
        _ => "Remove generated API",
    };

    public string PreferencesDialogTitle => Language switch
    {
        ExtensionLanguage.PortugueseBrazil => "Genexus Open API Builder - Preferências do Wizard",
        ExtensionLanguage.Spanish => "Genexus Open API Builder - Preferencias del Wizard",
        _ => "Genexus Open API Builder - Wizard Preferences",
    };

    public string SynchronizeDialogTitle => Language switch
    {
        ExtensionLanguage.PortugueseBrazil => "Genexus Open API Builder - Sincronizar com a Transaction",
        ExtensionLanguage.Spanish => "Genexus Open API Builder - Sincronizar con la Transaction",
        _ => "Genexus Open API Builder - Synchronize with the Transaction",
    };

    public string ApplySynchronization => Language switch
    {
        ExtensionLanguage.PortugueseBrazil => "Aplicar sincronização",
        ExtensionLanguage.Spanish => "Aplicar sincronización",
        _ => "Apply synchronization",
    };

    public string WizardTitle => "Genexus Open API Builder - Wizard";

    public string WizardContractTitle => "Genexus Open API Builder - Wizard B031";

    public string WizardReviewTitle => "Genexus Open API Builder - Wizard B032";

    public string FinalReportTitle => Language switch
    {
        ExtensionLanguage.PortugueseBrazil => "Genexus Open API Builder - Relatório final",
        ExtensionLanguage.Spanish => "Genexus Open API Builder - Informe final",
        _ => "Genexus Open API Builder - Final report",
    };

    public string Close => Language switch
    {
        ExtensionLanguage.PortugueseBrazil => "Fechar",
        ExtensionLanguage.Spanish => "Cerrar",
        _ => "Close",
    };

    public string Cancel => Language switch
    {
        ExtensionLanguage.PortugueseBrazil => "Cancelar",
        ExtensionLanguage.Spanish => "Cancelar",
        _ => "Cancel",
    };

    public string Next => Language switch
    {
        ExtensionLanguage.PortugueseBrazil => "Próximo",
        ExtensionLanguage.Spanish => "Siguiente",
        _ => "Next",
    };

    public string Back => Language switch
    {
        ExtensionLanguage.PortugueseBrazil => "Voltar",
        ExtensionLanguage.Spanish => "Atrás",
        _ => "Back",
    };

    public string Save => Language switch
    {
        ExtensionLanguage.PortugueseBrazil => "Salvar",
        ExtensionLanguage.Spanish => "Guardar",
        _ => "Save",
    };

    public string CompleteAndApply => Language switch
    {
        ExtensionLanguage.PortugueseBrazil => "Concluir e aplicar",
        ExtensionLanguage.Spanish => "Finalizar y aplicar",
        _ => "Complete and apply",
    };

    public string CompleteTest => Language switch
    {
        ExtensionLanguage.PortugueseBrazil => "Concluir teste",
        ExtensionLanguage.Spanish => "Finalizar prueba",
        _ => "Complete test",
    };

    public string OpenMainObject => Language switch
    {
        ExtensionLanguage.PortugueseBrazil => "Abrir objeto principal",
        ExtensionLanguage.Spanish => "Abrir objeto principal",
        _ => "Open main object",
    };

    public string RemovalConfirmationIntro => Language switch
    {
        ExtensionLanguage.PortugueseBrazil => "Remover API gerada apaga somente objetos próprios identificados pela metadata.",
        ExtensionLanguage.Spanish => "Eliminar API generada borra únicamente objetos propios identificados por los metadatos.",
        _ => "Remove generated API deletes only owned objects identified by metadata.",
    };

    public string ConfirmDeletion => Language switch
    {
        ExtensionLanguage.PortugueseBrazil => "Confirma a exclusão?",
        ExtensionLanguage.Spanish => "¿Confirma la eliminación?",
        _ => "Confirm deletion?",
    };

    public string Translate(string source)
    {
        if (Language == ExtensionLanguage.PortugueseBrazil || string.IsNullOrEmpty(source))
        {
            return source;
        }

        return Language == ExtensionLanguage.Spanish
            ? source switch
            {
                "Preferencias gerais do wizard na KB ativa" => "Preferencias generales del wizard en la KB activa",
                "Passo 2 - Configurar contrato" => "Paso 2 - Configurar contrato",
                "Passo 3 - Revisar paths e seguranca" => "Paso 3 - Revisar rutas y seguridad",
                "Paths" => "Rutas",
                "Seguranca" => "Seguridad",
                "Paginacao" => "Paginación",
                "Ordenacao" => "Ordenación",
                "Resumo B033" => "Resumen B033",
                "Services base path" => "Ruta base de servicios",
                "Resumo das decisoes acumuladas. B033 ainda nao executa nada na KB." => "Resumen de las decisiones acumuladas. B033 aún no ejecuta nada en la KB.",
                "Servicos" => "Servicios",
                "Serviços" => "Servicios",
                "Serviços REST do MVP. Todos iniciam habilitados." => "Servicios REST del MVP. Todos comienzan habilitados.",
                "Aba atual" => "Pestaña actual",
                "<nenhuma>" => "<ninguna>",
                "Plano da Transaction ainda nao consultado na KB." => "Plan de la Transaction aún no consultado en la KB.",
                "Requests" => "Solicitudes",
                "Response" => "Respuesta",
                "Filtros List" => "Filtros de List",
                "Resumo B032" => "Resumen B032",
                "Segurança" => "Seguridad",
                "Paginação" => "Paginación",
                "Ordenação" => "Ordenación",
                "Obrigatórios" => "Obligatorios",
                "Business Component" => "Business Component",
                "SDTs" => "SDTs",
                "Procedures" => "Procedures",
                "API Object" => "API Object",
                "Metadata" => "Metadatos",
                "List" => "List",
                "Resumo" => "Resumen",
                "Decisões" => "Decisiones",
                "Endpoints e garantias" => "Endpoints y garantías",
                "Paths dos serviços" => "Rutas de los servicios",
                "CreateRequest - Obrigatório no payload (editável)" => "CreateRequest - Obligatorio en el payload (editable)",
                "UpdateRequest - Obrigatório no payload" => "UpdateRequest - Obligatorio en el payload",
                "Revise os SDTs planejados. A escrita so sera executada ao concluir o wizard se esta confirmacao estiver marcada e o preflight tecnico estiver OK." => "Revise los SDTs planificados. La escritura solo se ejecutará al finalizar el wizard si esta confirmación está marcada y el preflight técnico está correcto.",
                "Revise as Procedures planejadas. Esta etapa depende das estruturas de dados ja confirmadas ou reencontraveis na KB ativa." => "Revise las Procedures planificadas. Esta etapa depende de las estructuras de datos ya confirmadas o recuperables en la KB activa.",
                "Revise o API Object planejado. Esta etapa depende das estruturas de dados e das Procedures ja confirmadas ou reencontraveis na KB ativa." => "Revise el API Object planificado. Esta etapa depende de las estructuras de datos y las Procedures ya confirmadas o recuperables en la KB activa.",
                "Revise o File JSON de metadata. A gravação depende do API Object próprio já confirmado ou reencontrado." => "Revise el archivo JSON de metadatos. La escritura depende del API Object propio ya confirmado o recuperado.",
                "Revise a listagem da API. A conclusão atualiza a Procedure de listagem e sincroniza o API Object com parâmetros de página, filtros e retorno paginado." => "Revise el listado de la API. La finalización actualiza la Procedure de listado y sincroniza el API Object con parámetros de página, filtros y respuesta paginada.",
                "CreateRequest" => "CreateRequest",
                "UpdateRequest" => "UpdateRequest",
                "Description" => "Descripción",
                "Sensivel" => "Sensible",
                "Formula" => "Fórmula",
                "Auditoria" => "Auditoría",
                "Periodo" => "Período",
                "Intervalo" => "Intervalo",
                "Resumo das decisoes acumuladas. B032 ainda nao executa nada na KB." => "Resumen de las decisiones acumuladas. B032 aún no ejecuta nada en la KB.",
                "Selecione ao menos um servico." => "Seleccione al menos un servicio.",
                "Defaults de geracao" => "Valores predeterminados de generación",
                "Servicos marcados por padrao" => "Servicios marcados por defecto",
                "Seguranca e paginacao" => "Seguridad y paginación",
                "Security Level" => "Nivel de seguridad",
                "Authentication" => "Autenticación",
                "Authorization" => "Autorización",
                "None" => "Ninguno",
                "Default Page Size" => "Tamaño de página predeterminado",
                "Maximum Page Size" => "Tamaño máximo de página",
                "Marcar SDTs por padrao" => "Marcar SDTs por defecto",
                "Marcar Procedures por padrao" => "Marcar Procedures por defecto",
                "Marcar API Object por padrao" => "Marcar API Object por defecto",
                "Marcar metadata da API por padrao" => "Marcar metadatos de la API por defecto",
                "Marcar listagem por padrao" => "Marcar listado por defecto",
                "Marcar Get/Create/Update REST por padrao" => "Marcar Get/Create/Update REST por defecto",
                "Habilitar Business Component agora" => "Habilitar Business Component ahora",
                "Confirmar: Criar ou validar estruturas de dados ao concluir" => "Confirmar: crear o validar estructuras de datos al finalizar",
                "Confirmar: Criar ou validar Procedures ao concluir" => "Confirmar: crear o validar Procedures al finalizar",
                "Confirmar: Criar ou validar API Object ao concluir" => "Confirmar: crear o validar API Object al finalizar",
                "Confirmar: Gravar metadata da API ao concluir" => "Confirmar: guardar los metadatos de la API al finalizar",
                "Completar Get/Create/Update REST ao concluir" => "Completar Get/Create/Update REST al finalizar",
                "Completar listagem ao concluir" => "Completar el listado al finalizar",
                "Marque ao menos um servico padrao." => "Marque al menos un servicio predeterminado.",
                "Confirmar" => "Confirmar",
                "Confirmar: Completar Get/Create/Update REST ao concluir" => "Confirmar: completar Get/Create/Update REST al finalizar",
                "ao concluir" => "al finalizar",
                "Bloqueado: confirme SDTs, Procedures e API Object" => "Bloqueado: confirme SDTs, Procedures y API Object",
                "Bloqueado: confirme SDTs, Procedures e API Object antes de aplicar BC" => "Bloqueado: confirme SDTs, Procedures y API Object antes de aplicar BC",
                "Confirmar: Completar Get/Create/Update REST após habilitar" => "Confirmar: completar Get/Create/Update REST después de habilitar",
                "Business Component está desabilitado. Marque a habilitação explícita para continuar ou cancele o wizard." => "Business Component está deshabilitado. Marque la habilitación explícita para continuar o cancele el wizard.",
                "Não foi possível confirmar Business Component habilitado após a gravação." => "No fue posible confirmar Business Component habilitado después de guardar.",
                "Falha ao habilitar Business Component: " => "Error al habilitar Business Component: ",
                "confirmada nesta execucao" => "confirmada en esta ejecución",
                "ja reencontrada na KB ativa" => "ya recuperada en la KB activa",
                "nao confirmada" => "no confirmada",
                "Estado: plano em memoria" => "Estado: plan en memoria",
                "Estado: teste bloqueado" => "Estado: prueba bloqueada",
                "conflito(s)" => "conflicto(s)",
                "Estado: teste de reencontro" => "Estado: prueba de recuperación",
                "Estado: teste de criacao" => "Estado: prueba de creación",
                "Estado: teste de complementacao" => "Estado: prueba de complementación",
                "estruturas de dados" => "estructuras de datos",
                "metadata da API" => "metadatos de la API",
                "Estado atual da KB indisponivel. Ajuste os campos obrigatorios do contrato para consultar a geracao." => "Estado actual de la KB no disponible. Ajuste los campos obligatorios del contrato para consultar la generación.",
                "Estado atual da KB" => "Estado actual de la KB",
                "Confirmado para escrita" => "Confirmado para escritura",
                "Sincronizar API com a Transaction" => "Sincronizar API con la Transaction",
                "Campos adicionados — marque onde incluir" => "Campos agregados — marque dónde incluir",
                "Conflitos de SDT editado manualmente" => "Conflictos de SDT editado manualmente",
                "Nenhum conflito de SDT detectado." => "No se detectaron conflictos de SDT.",
                "Avisos: remocoes e mudancas de tipo podem quebrar clientes; novo campo obrigatorio via BC pode quebrar Create." => "Avisos: las eliminaciones y los cambios de tipo pueden romper clientes; un campo nuevo obligatorio vía BC puede romper Create.",
                "Servicos REST do MVP. Todos iniciam habilitados." => "Servicios REST del MVP. Todos comienzan habilitados.",
                "Campos devolvidos no response principal." => "Campos devueltos en la respuesta principal.",
                "Filtros candidatos para o servico List." => "Filtros candidatos para el servicio List.",
                "Filtros candidatos para o serviço List." => "Filtros candidatos para el servicio List.",
                "Paths dos servicos" => "Rutas de los servicios",
                "Nome API" => "Nombre de la API",
                "Security Level unico aplicado aos servicos gerados no MVP." => "Nivel de seguridad único aplicado a los servicios generados en el MVP.",
                "Security Level único aplicado aos serviços gerados no MVP." => "Nivel de seguridad único aplicado a los servicios generados en el MVP.",
                "Authentication inicia selecionado por seguranca. None permanece apenas como decisao prototipica nesta etapa." => "Authentication comienza seleccionado por seguridad. None permanece solo como decisión prototípica en esta etapa.",
                "Authentication inicia selecionado por segurança. Authorization exige permissões GAM coerentes. None deixa a API pública e exigirá confirmação antes da geração." => "Authentication comienza seleccionado por seguridad. Authorization requiere permisos GAM coherentes. None deja la API pública y requerirá confirmación antes de la generación.",
                "Ordenacao estatica inicial. A chave primaria completa e acrescentada como desempate ascendente." => "Ordenación estática inicial. La clave primaria completa se añade como desempate ascendente.",
                "Ordenação estática inicial. A chave primária completa é acrescentada como desempate ascendente." => "Ordenación estática inicial. La clave primaria completa se añade como desempate ascendente.",
                "Required marca membro obrigatório no payload: Create/Update respondem 400 quando ele chega ausente ou com o valor default do tipo (vazio, false ou 0). Chave primária não autonumerada inicia opcional no Create; marque aqui se quiser exigir o valor no payload." => "Required marca el miembro obligatorio en el payload: Create/Update responden 400 cuando llega ausente o con el valor predeterminado del tipo (vacío, false o 0). La clave primaria no autonumerada comienza opcional en Create; márquela aquí si desea exigir el valor en el payload.",
                "Business Component preserva as regras da Transaction. A confirmação abaixo completa Get, Create e Update nas Procedures já geradas e sincroniza o API Object; não cria novos objetos." => "Business Component conserva las reglas de la Transaction. La confirmación completa Get, Create y Update en las Procedures ya generadas y sincroniza el API Object; no crea objetos nuevos.",
                "SDTs planejados" => "SDTs planificados",
                "Procedures planejadas" => "Procedures planificadas",
                "API Object planejado" => "API Object planificado",
                "File de metadata planejado" => "Archivo de metadatos planificado",
                "List planejado" => "List planificado",
                "Resumo das decisões acumuladas para montagem do ApiPlan em memória." => "Resumen de las decisiones acumuladas para montar el ApiPlan en memoria.",
                "Selecione ao menos um serviço." => "Seleccione al menos un servicio.",
                "Informe Nome API, Services base path e RestPath." => "Informe Nombre de la API, ruta base de servicios y RestPath.",
                "RestPath deve iniciar com '/'." => "RestPath debe comenzar con '/'.",
                "Default Page Size deve ser menor ou igual a Maximum Page Size." => "El tamaño de página predeterminado debe ser menor o igual que el tamaño máximo de página.",
                "Estado atual indisponivel" => "Estado actual no disponible",
                "Bloqueado: Business Component desabilitado" => "Bloqueado: Business Component deshabilitado",
                "Confirmar: Completar listagem ao concluir" => "Confirmar: completar el listado al finalizar",
                "Período" => "Período",
                "Sensível" => "Sensible",
                "Fórmula" => "Fórmula",
                _ => source,
            }
            : source switch
            {
                "Preferencias gerais do wizard na KB ativa" => "General wizard preferences in the active KB",
                "Passo 2 - Configurar contrato" => "Step 2 - Configure contract",
                "Passo 3 - Revisar paths e seguranca" => "Step 3 - Review paths and security",
                "Paths" => "Paths",
                "Seguranca" => "Security",
                "Paginacao" => "Pagination",
                "Ordenacao" => "Ordering",
                "Resumo B033" => "B033 summary",
                "Resumo das decisoes acumuladas. B033 ainda nao executa nada na KB." => "Summary of accumulated decisions. B033 does not execute anything in the KB yet.",
                "Servicos" => "Services",
                "Serviços" => "Services",
                "Serviços REST do MVP. Todos iniciam habilitados." => "MVP REST services. All start enabled.",
                "Aba atual" => "Current tab",
                "<nenhuma>" => "<none>",
                "Plano da Transaction ainda nao consultado na KB." => "Transaction plan not yet read from the KB.",
                "Requests" => "Requests",
                "Response" => "Response",
                "Filtros List" => "List filters",
                "Resumo B032" => "B032 summary",
                "Segurança" => "Security",
                "Paginação" => "Pagination",
                "Ordenação" => "Ordering",
                "Obrigatórios" => "Required fields",
                "Business Component" => "Business Component",
                "SDTs" => "SDTs",
                "Procedures" => "Procedures",
                "API Object" => "API Object",
                "Metadata" => "Metadata",
                "List" => "List",
                "Resumo" => "Summary",
                "Decisões" => "Decisions",
                "Endpoints e garantias" => "Endpoints and guarantees",
                "Paths dos serviços" => "Service paths",
                "CreateRequest - Obrigatório no payload (editável)" => "CreateRequest - Required in payload (editable)",
                "UpdateRequest - Obrigatório no payload" => "UpdateRequest - Required in payload",
                "Revise os SDTs planejados. A escrita so sera executada ao concluir o wizard se esta confirmacao estiver marcada e o preflight tecnico estiver OK." => "Review the planned SDTs. Writing occurs on wizard completion only when this confirmation is selected and the technical preflight is successful.",
                "Revise as Procedures planejadas. Esta etapa depende das estruturas de dados ja confirmadas ou reencontraveis na KB ativa." => "Review the planned Procedures. This step depends on data structures already confirmed or recoverable in the active KB.",
                "Revise o API Object planejado. Esta etapa depende das estruturas de dados e das Procedures ja confirmadas ou reencontraveis na KB ativa." => "Review the planned API Object. This step depends on data structures and Procedures already confirmed or recoverable in the active KB.",
                "Revise o File JSON de metadata. A gravação depende do API Object próprio já confirmado ou reencontrado." => "Review the metadata JSON file. Writing depends on the owned API Object already confirmed or recovered.",
                "Revise a listagem da API. A conclusão atualiza a Procedure de listagem e sincroniza o API Object com parâmetros de página, filtros e retorno paginado." => "Review the API List endpoint. Completion updates the List Procedure and synchronizes the API Object with page parameters, filters, and paginated response.",
                "CreateRequest" => "CreateRequest",
                "UpdateRequest" => "UpdateRequest",
                "Description" => "Description",
                "Sensivel" => "Sensitive",
                "Formula" => "Formula",
                "Auditoria" => "Audit",
                "Periodo" => "Period",
                "Intervalo" => "Range",
                "Resumo das decisoes acumuladas. B032 ainda nao executa nada na KB." => "Summary of accumulated decisions. B032 does not execute anything in the KB yet.",
                "Selecione ao menos um servico." => "Select at least one service.",
                "Defaults de geracao" => "Generation defaults",
                "Servicos marcados por padrao" => "Services selected by default",
                "Seguranca e paginacao" => "Security and pagination",
                "Security Level" => "Security level",
                "Authentication" => "Authentication",
                "Authorization" => "Authorization",
                "None" => "None",
                "Default Page Size" => "Default page size",
                "Maximum Page Size" => "Maximum page size",
                "Marcar SDTs por padrao" => "Select SDTs by default",
                "Marcar Procedures por padrao" => "Select Procedures by default",
                "Marcar API Object por padrao" => "Select API Object by default",
                "Marcar metadata da API por padrao" => "Select API metadata by default",
                "Marcar listagem por padrao" => "Select list endpoint by default",
                "Marcar Get/Create/Update REST por padrao" => "Select Get/Create/Update REST by default",
                "Habilitar Business Component agora" => "Enable Business Component now",
                "Confirmar: Criar ou validar estruturas de dados ao concluir" => "Confirm: create or validate data structures on finish",
                "Confirmar: Criar ou validar Procedures ao concluir" => "Confirm: create or validate Procedures on finish",
                "Confirmar: Criar ou validar API Object ao concluir" => "Confirm: create or validate API Object on finish",
                "Confirmar: Gravar metadata da API ao concluir" => "Confirm: save API metadata on finish",
                "Completar Get/Create/Update REST ao concluir" => "Complete Get/Create/Update REST on finish",
                "Completar listagem ao concluir" => "Complete the List endpoint on finish",
                "Marque ao menos um servico padrao." => "Select at least one default service.",
                "Confirmar" => "Confirm",
                "Confirmar: Completar Get/Create/Update REST ao concluir" => "Confirm: complete Get/Create/Update REST on finish",
                "ao concluir" => "on finish",
                "Bloqueado: confirme SDTs, Procedures e API Object" => "Blocked: confirm SDTs, Procedures, and API Object",
                "Bloqueado: confirme SDTs, Procedures e API Object antes de aplicar BC" => "Blocked: confirm SDTs, Procedures, and API Object before applying BC",
                "Confirmar: Completar Get/Create/Update REST após habilitar" => "Confirm: complete Get/Create/Update REST after enabling",
                "Business Component está desabilitado. Marque a habilitação explícita para continuar ou cancele o wizard." => "Business Component is disabled. Select explicit enablement to continue or cancel the wizard.",
                "Não foi possível confirmar Business Component habilitado após a gravação." => "Could not confirm that Business Component was enabled after saving.",
                "Falha ao habilitar Business Component: " => "Failed to enable Business Component: ",
                "confirmada nesta execucao" => "confirmed in this run",
                "ja reencontrada na KB ativa" => "already recovered in the active KB",
                "nao confirmada" => "not confirmed",
                "Estado: plano em memoria" => "Status: plan in memory",
                "Estado: teste bloqueado" => "Status: test blocked",
                "conflito(s)" => "conflict(s)",
                "Estado: teste de reencontro" => "Status: recovery test",
                "Estado: teste de criacao" => "Status: creation test",
                "Estado: teste de complementacao" => "Status: completion test",
                "estruturas de dados" => "data structures",
                "metadata da API" => "API metadata",
                "Estado atual da KB indisponivel. Ajuste os campos obrigatorios do contrato para consultar a geracao." => "Current KB state unavailable. Adjust the contract's required fields to inspect generation.",
                "Estado atual da KB" => "Current KB state",
                "Confirmado para escrita" => "Confirmed for writing",
                "Sincronizar API com a Transaction" => "Synchronize API with the Transaction",
                "Campos adicionados — marque onde incluir" => "Added fields — select where to include them",
                "Conflitos de SDT editado manualmente" => "Manually edited SDT conflicts",
                "Nenhum conflito de SDT detectado." => "No SDT conflicts detected.",
                "Avisos: remocoes e mudancas de tipo podem quebrar clientes; novo campo obrigatorio via BC pode quebrar Create." => "Warnings: removals and type changes can break clients; a new required field through BC can break Create.",
                "Servicos REST do MVP. Todos iniciam habilitados." => "MVP REST services. All start enabled.",
                "Campos devolvidos no response principal." => "Fields returned in the main response.",
                "Filtros candidatos para o servico List." => "Candidate filters for the List service.",
                "Filtros candidatos para o serviço List." => "Candidate filters for the List service.",
                "Paths dos servicos" => "Service paths",
                "Nome API" => "API name",
                "Services base path" => "Services base path",
                "Security Level unico aplicado aos servicos gerados no MVP." => "Single security level applied to services generated in the MVP.",
                "Security Level único aplicado aos serviços gerados no MVP." => "Single security level applied to services generated in the MVP.",
                "Authentication inicia selecionado por seguranca. None permanece apenas como decisao prototipica nesta etapa." => "Authentication starts selected for security. None remains only as a prototype decision at this stage.",
                "Authentication inicia selecionado por segurança. Authorization exige permissões GAM coerentes. None deixa a API pública e exigirá confirmação antes da geração." => "Authentication starts selected for security. Authorization requires consistent GAM permissions. None leaves the API public and requires confirmation before generation.",
                "Ordenacao estatica inicial. A chave primaria completa e acrescentada como desempate ascendente." => "Initial static ordering. The complete primary key is added as an ascending tiebreaker.",
                "Ordenação estática inicial. A chave primária completa é acrescentada como desempate ascendente." => "Initial static ordering. The complete primary key is added as an ascending tiebreaker.",
                "Required marca membro obrigatório no payload: Create/Update respondem 400 quando ele chega ausente ou com o valor default do tipo (vazio, false ou 0). Chave primária não autonumerada inicia opcional no Create; marque aqui se quiser exigir o valor no payload." => "Required marks a member as mandatory in the payload: Create/Update return 400 when it is absent or has the type default value (empty, false, or 0). A non-autonumbered primary key starts optional in Create; select it here to require it in the payload.",
                "Business Component preserva as regras da Transaction. A confirmação abaixo completa Get, Create e Update nas Procedures já geradas e sincroniza o API Object; não cria novos objetos." => "Business Component preserves the Transaction rules. The confirmation completes Get, Create, and Update in the generated Procedures and synchronizes the API Object; it does not create new objects.",
                "SDTs planejados" => "Planned SDTs",
                "Procedures planejadas" => "Planned Procedures",
                "API Object planejado" => "Planned API Object",
                "File de metadata planejado" => "Planned metadata file",
                "List planejado" => "Planned List",
                "Resumo das decisões acumuladas para montagem do ApiPlan em memória." => "Summary of accumulated decisions for building the ApiPlan in memory.",
                "Selecione ao menos um serviço." => "Select at least one service.",
                "Informe Nome API, Services base path e RestPath." => "Enter the API name, services base path, and RestPath.",
                "RestPath deve iniciar com '/'." => "RestPath must start with '/'.",
                "Default Page Size deve ser menor ou igual a Maximum Page Size." => "Default page size must be less than or equal to maximum page size.",
                "Estado atual indisponivel" => "Current state unavailable",
                "Bloqueado: Business Component desabilitado" => "Blocked: Business Component disabled",
                "Confirmar: Completar listagem ao concluir" => "Confirm: complete the List endpoint on finish",
                "Período" => "Period",
                "Sensível" => "Sensitive",
                "Fórmula" => "Formula",
                _ => source,
            };
    }
}

internal static class ExtensionLocalization
{
    public static ExtensionTexts For(KnowledgeBase? knowledgeBase)
    {
        return new ExtensionTexts(Resolve(knowledgeBase));
    }

    public static ExtensionLanguage Resolve(KnowledgeBase? knowledgeBase)
    {
        if (knowledgeBase is null)
        {
            return ExtensionLanguage.English;
        }

        foreach (var rawValue in ReadLanguageValues(knowledgeBase))
        {
            Language? language = null;
            try
            {
                language = FindLanguage(knowledgeBase.DesignModel, rawValue);
            }
            catch
            {
                // O valor bruto ainda pode ser uma tag/nome diretamente reconhecível.
            }

            if (language is not null)
            {
                // Se a KB expõe um idioma conhecido, o IETF tag tem precedência
                // sobre o texto de apresentação (por exemplo, pt-PT).
                return ExtensionLanguageResolver.Resolve(
                    language.Name,
                    language.IETFLanguageTag,
                    Convert.ToString(rawValue, CultureInfo.InvariantCulture));
            }

            var resolvedFromRawValue = ExtensionLanguageResolver.Resolve(
                languageName: null,
                ietfLanguageTag: null,
                rawValue: Convert.ToString(rawValue, CultureInfo.InvariantCulture));
            if (resolvedFromRawValue != ExtensionLanguage.English)
            {
                return resolvedFromRawValue;
            }
        }

        return ExtensionLanguage.English;
    }

    public static bool IsCurrentKnowledgeBase(ExtensionLanguage expected)
    {
        var knowledgeBase = UIServices.IsKBAvailable ? UIServices.KB.CurrentKB : null;
        return Resolve(knowledgeBase) == expected;
    }

    private static Language? FindLanguage(KBModel designModel, object? rawValue)
    {
        if (rawValue is Language languageValue)
        {
            return languageValue;
        }

        var rawText = Convert.ToString(rawValue, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        if (int.TryParse(rawText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var languageId))
        {
            try
            {
                var byId = Language.Get(designModel, languageId);
                if (byId is not null)
                {
                    return byId;
                }
            }
            catch
            {
                // Continua com a busca por nome ou tag.
            }
        }

        try
        {
            var direct = Language.Get(designModel, rawText);
            if (direct is not null)
            {
                return direct;
            }
        }
        catch
        {
            // Continua com a busca por ID, nome ou tag entre os idiomas carregados.
        }

        IEnumerable<Language> languages;
        try
        {
            languages = Language.GetAll(designModel);
        }
        catch
        {
            return null;
        }

        foreach (var language in languages)
        {
            if (string.Equals(language.Id.ToString(CultureInfo.InvariantCulture), rawText, StringComparison.OrdinalIgnoreCase)
                || string.Equals(language.Name, rawText, StringComparison.OrdinalIgnoreCase)
                || string.Equals(language.IETFLanguageTag, rawText, StringComparison.OrdinalIgnoreCase))
            {
                return language;
            }
        }

        return null;
    }

    private static IEnumerable<object?> ReadLanguageValues(KnowledgeBase knowledgeBase)
    {
        var values = new List<object?>();
        var properties = knowledgeBase.Properties;
        var propertyName = Artech.Genexus.Common.Properties.KB.KbLanguage;

        TryAddPropertyValue(values, () => properties.GetPropertyValue(propertyName));
        TryAddPropertyValue(values, () => properties[propertyName]);
        TryAddPropertyValue(values, () => properties.GetStoredPropertyValue(propertyName));
        TryAddPropertyValue(values, () => properties.GetPropertyValueString(propertyName));

        return values;
    }

    private static void TryAddPropertyValue(ICollection<object?> values, Func<object?> readValue)
    {
        try
        {
            var value = readValue();
            if (value is not null)
            {
                values.Add(value);
            }
        }
        catch
        {
            // Uma forma de leitura pode não estar disponível em todas as versões da IDE.
        }
    }
}
