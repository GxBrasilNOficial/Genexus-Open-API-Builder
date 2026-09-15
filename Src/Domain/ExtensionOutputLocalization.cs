#nullable enable

using System;
using System.Linq;

namespace GenexusOpenApiBuilder.Extension.Domain;

internal static class ExtensionOutputLocalization
{
    private sealed class Phrase
    {
        public Phrase(string source, string spanish, string english)
        {
            Source = source;
            Spanish = spanish;
            English = english;
        }

        public string Source { get; }

        public string Spanish { get; }

        public string English { get; }
    }

    private static readonly Phrase[] Phrases =
    {
        // B111/F3 P8: as duas orientações do descompasso de `ownership.apiGuid`. Elas vêm antes
        // das demais porque são frases longas: a substituição é por substring, e um fragmento
        // curto cadastrado antes recortaria o meio delas.
        new(
            " registra um API Object que não existe mais na KB, ou outro que não é o desta aplicação. Para regerar a API a partir do que restou na KB, apague esse File e o API Object que leva o nome da API — os dois, porque um sem o outro apenas troca este bloqueio pelo seguinte — e execute o Wizard de novo: os SDTs e as Procedures existentes são reencontrados, e o API Object e a metadata são recriados. Paginação, ordenação e campos obrigatórios voltam aos padrões das preferências, porque só existiam na metadata apagada. Nenhuma alteração foi feita.",
            " registra un API Object que ya no existe en la KB, u otro que no es el de esta aplicación. Para regenerar la API a partir de lo que quedó en la KB, borre ese File y el API Object que lleva el nombre de la API — los dos, porque uno sin el otro solo cambia este bloqueo por el siguiente — y ejecute el Wizard de nuevo: los SDTs y los Procedures existentes se reencuentran, y el API Object y los metadatos se recrean. Paginación, ordenación y campos obligatorios vuelven a los valores predeterminados de las preferencias, porque solo existían en los metadatos borrados. No se modificó nada.",
            " records an API Object that no longer exists in the KB, or a different one that does not belong to this application. To regenerate the API from what is left in the KB, delete that File and the API Object that carries the API name — both, because one without the other only swaps this block for the next one — and run the Wizard again: existing SDTs and Procedures are rediscovered, and the API Object and the metadata are recreated. Pagination, ordering, and required fields fall back to the preference defaults, because they only existed in the deleted metadata. No changes were made."),
        new(
            " O API Object previsto não está na KB, e quem o apagou não foi esta operação. Há duas saídas, e a escolha é sua: para regerar a API sobre o que restou, apague o File",
            " El API Object previsto no está en la KB, y quien lo borró no fue esta operación. Hay dos salidas, y la elección es suya: para regenerar la API sobre lo que quedó, borre el File",
            " The planned API Object is not in the KB, and this operation is not the one that deleted it. There are two ways out, and the choice is yours: to regenerate the API over what is left, delete the File"),
        new(
            " e reaplique pelo Wizard, que reencontra SDTs e Procedures — paginação, ordenação e campos obrigatórios voltam aos padrões das preferências; para descartar o que restou, apague os objetos listados acima pela KB Explorer.",
            " y vuelva a aplicar por el Wizard, que reencuentra SDTs y Procedures — paginación, ordenación y campos obligatorios vuelven a los valores predeterminados de las preferencias; para descartar lo que quedó, borre los objetos listados arriba por el KB Explorer.",
            " and reapply through the Wizard, which rediscovers SDTs and Procedures — pagination, ordering, and required fields fall back to the preference defaults; to discard what is left, delete the objects listed above through the KB Explorer."),
        // B111/F3 P8: os desfechos e resumos da recuperação. Eles nascem no executor e no
        // rehydrator, que são SDK-simples e não conhecem idioma; sem estas entradas, saíam em
        // português em qualquer KB — a única parte da recuperação que a P7 não tinha coberto.
        new(
            "O registro da operação interrompida foi encerrado. A Knowledge Base está liberada para a próxima operação e continua como estava antes dela.",
            "El registro de la operación interrumpida fue cerrado. La Knowledge Base está liberada para la próxima operación y sigue como estaba antes de ella.",
            "The record of the interrupted operation was closed. The Knowledge Base is free for the next operation and remains as it was before it."),
        new(
            "O registro da operação interrompida foi encerrado. A Knowledge Base está liberada para a próxima operação; nenhum objeto foi apagado, e o inventário do que ficou pela metade continua gravado no diário.",
            "El registro de la operación interrumpida fue cerrado. La Knowledge Base está liberada para la próxima operación; ningún objeto fue borrado, y el inventario de lo que quedó a medias sigue grabado en el diario.",
            "The record of the interrupted operation was closed. The Knowledge Base is free for the next operation; no object was deleted, and the inventory of what was left halfway remains recorded in the journal."),
        new(
            "O envelope preparado foi abandonado. A KB está liberada para a próxima operação.",
            "El sobre preparado fue abandonado. La KB está liberada para la próxima operación.",
            "The prepared envelope was abandoned. The KB is free for the next operation."),
        new(
            "A remoção foi reconciliada: todos os alvos previstos estão ausentes e o envelope fechou como concluído.",
            "La eliminación fue reconciliada: todos los objetivos previstos están ausentes y el sobre cerró como concluido.",
            "The removal was reconciled: every planned target is absent and the envelope closed as completed."),
        new(
            " foi registrada e interrompida antes de gravar qualquer objeto: o diário não tem nenhum recibo. Encerrar este registro libera a Knowledge Base, que está como estava antes desta operação.",
            " fue registrada e interrumpida antes de grabar cualquier objeto: el diario no tiene ningún recibo. Cerrar este registro libera la Knowledge Base, que está como estaba antes de esta operación.",
            " was recorded and interrupted before writing any object: the journal has no receipts. Closing this record frees the Knowledge Base, which is as it was before this operation."),
        // B111/F3 P7 (dívida fechada na P8): os bloqueios, resumos e recusas da recuperação.
        // O rehydrator e o executor são SDK-simples e não conhecem idioma; sem estas entradas,
        // tudo o que o diálogo de recuperação diz saía em português em qualquer KB. As frases
        // que carregam enum no meio entram partidas, porque a substituição é por substring e
        // o miolo variável não pode ser cadastrado.
        new(
            "A durabilidade do último snapshot do diário não foi confirmada. Reconcilie a operação antes de qualquer continuação: uma gravação incerta não vira certeza por repetição.",
            "La durabilidad del último snapshot del diario no fue confirmada. Reconcilie la operación antes de cualquier continuación: una grabación incierta no se vuelve certeza por repetición.",
            "The durability of the last journal snapshot was not confirmed. Reconcile the operation before any continuation: an uncertain write does not become certain by repetition."),
        new(
            "O envelope está Prepared, mas registra recibos de gravação. Abandoná-lo apagaria a única prova do que foi gravado; a reconciliação é humana.",
            "El sobre está Prepared, pero registra recibos de grabación. Abandonarlo borraría la única prueba de lo que fue grabado; la reconciliación es humana.",
            "The envelope is Prepared, but it records write receipts. Abandoning it would erase the only proof of what was written; reconciliation is human."),
        new(
            "A operação foi preparada e nunca gravou nada na KB. Abandoná-la explicitamente libera a KB para a próxima operação, preservando o registro da disposição.",
            "La operación fue preparada y nunca grabó nada en la KB. Abandonarla explícitamente libera la KB para la próxima operación, preservando el registro de la disposición.",
            "The operation was prepared and never wrote anything to the KB. Abandoning it explicitly frees the KB for the next operation, preserving the record of the decision."),
        new(
            "A última gravação terminou com resultado desconhecido. A consulta por identidade precisa ser feita e conferida por uma pessoa antes de qualquer nova gravação.",
            "La última grabación terminó con resultado desconocido. La consulta por identidad debe ser hecha y verificada por una persona antes de cualquier nueva grabación.",
            "The last write ended with an unknown result. The identity lookup must be performed and checked by a person before any new write."),
        new(
            " parou em ",
            " se detuvo en ",
            " stopped at "),
        new(
            ". O diário registra identidade, contrato por hash e o que já foi confirmado — não o contrato em si —, então retomar o pipeline a partir dele seria inventar um plano. O que a ferramenta pode fazer é encerrar este registro: a Knowledge Base é liberada, nada é apagado, e o que ficou pela metade continua como está. Depois disso, as duas saídas são reaplicar pelo Wizard sobre o estado atual — o reencontro conservador cuida do que já existe — ou remover a API gerada.",
            ". El diario registra identidad, contrato por hash y lo que ya fue confirmado — no el contrato en sí —, entonces retomar el pipeline a partir de él sería inventar un plan. Lo que la herramienta puede hacer es cerrar este registro: la Knowledge Base queda liberada, nada es borrado, y lo que quedó a medias continúa como está. Después de eso, las dos salidas son volver a aplicar por el Wizard sobre el estado actual — el reencuentro conservador se ocupa de lo que ya existe — o eliminar la API generada.",
            ". The journal records identity, the contract by hash, and what was already confirmed — not the contract itself —, so resuming the pipeline from it would mean inventing a plan. What the tool can do is close this record: the Knowledge Base is freed, nothing is deleted, and whatever was left halfway stays as it is. After that, the two ways out are reapplying through the Wizard over the current state — conservative rediscovery handles what already exists — or removing the generated API."),
        new(
            " alvo(s) previsto(s) não puderam ser lidos por identidade: ",
            " objetivo(s) previsto(s) no pudieron ser leídos por identidad: ",
            " planned target(s) could not be read by identity: "),
        new(
            ". Sem saber se ainda estão na KB, a fila não pode ser retomada.",
            ". Sin saber si todavía están en la KB, la cola no puede ser retomada.",
            ". Without knowing whether they are still in the KB, the queue cannot be resumed."),
        new(
            "Todos os alvos previstos estão ausentes da KB: a remoção chegou ao fim e só o registro ficou aberto. Fechar o envelope como concluído reconcilia o diário com a KB.",
            "Todos los objetivos previstos están ausentes de la KB: la eliminación llegó al final y solo el registro quedó abierto. Cerrar el sobre como concluido reconcilia el diario con la KB.",
            "Every planned target is absent from the KB: the removal reached its end and only the record stayed open. Closing the envelope as completed reconciles the journal with the KB."),
        new(
            "A remoção parou porque um alvo previsto já não estava na KB antes da exclusão, e quem o apagou não foi esta operação: retomar a fila às cegas não é possível. ",
            "La eliminación se detuvo porque un objetivo previsto ya no estaba en la KB antes de la exclusión, y quien lo borró no fue esta operación: retomar la cola a ciegas no es posible. ",
            "The removal stopped because a planned target was no longer in the KB before deletion, and this operation is not the one that deleted it: resuming the queue blindly is not possible. "),
        new(
            " alvo(s) previstos ainda estão na KB, listados abaixo. Encerrar este registro libera a Knowledge Base e não apaga nada; o que estiver pela metade continua como está, para você decidir depois.",
            " objetivo(s) previstos todavía están en la KB, listados abajo. Cerrar este registro libera la Knowledge Base y no borra nada; lo que esté a medias continúa como está, para que usted decida después.",
            " planned target(s) are still in the KB, listed below. Closing this record frees the Knowledge Base and deletes nothing; whatever is halfway stays as it is, for you to decide later."),
        new(
            " alvo(s) previsto(s) continuam na KB, listados abaixo. A fila pode ser retomada com o mesmo inventário e o mesmo envelope.",
            " objetivo(s) previsto(s) siguen en la KB, listados abajo. La cola puede ser retomada con el mismo inventario y el mismo sobre.",
            " planned target(s) are still in the KB, listed below. The queue can be resumed with the same inventory and the same envelope."),
        new(
            " já está encerrada em ",
            " ya está cerrada en ",
            " is already closed at "),
        new(
            ". Não há nada a recuperar; a KB está liberada para a próxima operação.",
            ". No hay nada que recuperar; la KB está liberada para la próxima operación.",
            ". There is nothing to recover; the KB is free for the next operation."),
        new(
            " — previsto: ",
            " — previsto: ",
            " — planned: "),
        new(
            "; na KB: ",
            "; en la KB: ",
            "; in the KB: "),
        new(
            "Não há etapa executável para este envelope.",
            "No hay etapa ejecutable para este sobre.",
            "There is no executable step for this envelope."),
        new(
            "Outra continuação desta KB está em andamento nesta sessão. A execução não prossegue como se houvesse atomicidade.",
            "Otra continuación de esta KB está en curso en esta sesión. La ejecución no prosigue como si hubiera atomicidad.",
            "Another continuation of this KB is in progress in this session. Execution does not proceed as if atomicity existed."),
        new(
            "A retomada da fila de remoção não foi fornecida por quem chamou a recuperação.",
            "La reanudación de la cola de eliminación no fue provista por quien llamó a la recuperación.",
            "The removal queue continuation was not supplied by the caller of the recovery."),
        new(
            "Etapa sem execução disponível nesta versão: ",
            "Etapa sin ejecución disponible en esta versión: ",
            "Step with no execution available in this version: "),
        new(
            "A transição autorizada não é válida no estado revalidado: ",
            "La transición autorizada no es válida en el estado revalidado: ",
            "The authorized transition is not valid in the revalidated state: "),
        new(
            "A execução exige confirmação humana explícita.",
            "La ejecución exige confirmación humana explícita.",
            "Execution requires explicit human confirmation."),
        new(
            "A autorização é para '",
            "La autorización es para '",
            "The authorization is for '"),
        new(
            "', e a etapa apurada agora é '",
            "', y la etapa determinada ahora es '",
            "', and the step determined now is '"),
        new(
            "Os identificadores da autorização não são os do envelope revalidado.",
            "Los identificadores de la autorización no son los del sobre revalidado.",
            "The authorization identifiers are not those of the revalidated envelope."),
        new(
            "O diário revalidado está em outro File.",
            "El diario revalidado está en otro File.",
            "The revalidated journal is in a different File."),
        new(
            "O envelope foi atualizado depois da leitura que produziu a autorização.",
            "El sobre fue actualizado después de la lectura que produjo la autorización.",
            "The envelope was updated after the read that produced the authorization."),
        new(
            "O hash canônico do snapshot mudou entre a leitura e a ação.",
            "El hash canónico del snapshot cambió entre la lectura y la acción.",
            "The canonical snapshot hash changed between the read and the action."),
        // B111/F3 P7 (dívida fechada na P8): o gate estendido e o store do diário. Eles são
        // SDK-simples e neutros de propósito, e a mensagem que produzem chega ao relatório
        // final — que localiza pelo catálogo — e à Output. Sem estas entradas, todo bloqueio
        // por diário saía em português em qualquer KB. As frases com enum no meio entram
        // partidas; ' em estado ' cobre as duas irmãs, que foram alinhadas no gate para isso.
        new(
            "O diário encontrado pertence à KB '",
            "El diario encontrado pertenece a la KB '",
            "The journal found belongs to KB '"),
        new(
            "', e a KB aberta é '",
            "', y la KB abierta es '",
            "', and the open KB is '"),
        new(
            "'. Um diário de outra KB não governa esta.",
            "'. Un diario de otra KB no gobierna esta.",
            "'. A journal from another KB does not govern this one."),
        new(
            "A durabilidade do diário atual não pôde ser confirmada; a operação anterior precisa ser reconciliada antes de uma nova.",
            "La durabilidad del diario actual no pudo ser confirmada; la operación anterior debe ser reconciliada antes de una nueva.",
            "The durability of the current journal could not be confirmed; the previous operation must be reconciled before a new one."),
        new(
            "O diário da KB registra a operação ",
            "El diario de la KB registra la operación ",
            "The KB journal records the "),
        new(
            " preparada e ainda não iniciada. Continuá-la ou abandoná-la exige autorização explícita.",
            " preparada y aún no iniciada. Continuarla o abandonarla exige autorización explícita.",
            " operation as prepared and not yet started. Continuing or abandoning it requires explicit authorization."),
        new(
            " em estado ",
            " en estado ",
            " operation in state "),
        new(
            ", que não é terminal. Reconcilie ou continue essa operação antes de iniciar outra.",
            ", que no es terminal. Reconcilie o continúe esa operación antes de iniciar otra.",
            ", which is not terminal. Reconcile or continue that operation before starting another."),
        new(
            ": o resultado da última gravação não é conhecido e precisa ser reconciliado por identidade antes de outra operação.",
            ": el resultado de la última grabación no es conocido y debe ser reconciliado por identidad antes de otra operación.",
            ": the result of the last write is not known and must be reconciled by identity before another operation."),
        new(
            "A autorização de continuação é da operação '",
            "La autorización de continuación es de la operación '",
            "The continuation authorization is for operation '"),
        new(
            "', e o diário registra '",
            "', y el diario registra '",
            "', and the journal records '"),
        new(
            "A continuação do envelope preparado foi autorizada, mas o serviço que a executa ainda não existe.",
            "La continuación del sobre preparado fue autorizada, pero el servicio que la ejecuta todavía no existe.",
            "The continuation of the prepared envelope was authorized, but the service that executes it does not exist yet."),
        new(
            "] Pré-condição '",
            "] Precondición '",
            "] Precondition '"),
        new(
            " Contexto: ",
            " Contexto: ",
            " Context: "),
        new(
            "O File do diário não tem conteúdo persistido.",
            "El File del diario no tiene contenido persistido.",
            "The journal File has no persisted content."),
        new(
            "O Save() do diário lançou: ",
            "El Save() del diario lanzó: ",
            "The journal Save() threw: "),
        new(
            "O File do diário não devolveu um Id utilizável após o Save().",
            "El File del diario no devolvió un Id utilizable después del Save().",
            "The journal File did not return a usable Id after Save()."),
        new(
            "A releitura do diário por FileId falhou: ",
            "La relectura del diario por FileId falló: ",
            "Re-reading the journal by FileId failed: "),
        new(
            "A releitura do diário por FileId não encontrou o File.",
            "La relectura del diario por FileId no encontró el File.",
            "Re-reading the journal by FileId did not find the File."),
        new(
            "O FileId ",
            "El FileId ",
            "FileId "),
        new(
            " resolveu para '",
            " resolvió a '",
            " resolved to '"),
        new(
            "', e não para o diário.",
            "', y no al diario.",
            "', not to the journal."),
        new(
            "O diário relido não tem conteúdo persistido.",
            "El diario releído no tiene contenido persistido.",
            "The re-read journal has no persisted content."),
        new(
            "Os bytes relidos do diário divergem do snapshot gravado.",
            "Los bytes releídos del diario difieren del snapshot grabado.",
            "The journal bytes read back differ from the written snapshot."),
        new(
            "O digest dos bytes relidos diverge do snapshot gravado.",
            "El digest de los bytes releídos difiere del snapshot grabado.",
            "The digest of the bytes read back differs from the written snapshot."),
        new(
            "O snapshot relido não reproduz o hash canônico esperado.",
            "El snapshot releído no reproduce el hash canónico esperado.",
            "The snapshot read back does not reproduce the expected canonical hash."),
        new(
            "Foram encontrados ",
            "Se encontraron ",
            "Found "),
        new(
            " Files chamados '",
            " Files llamados '",
            " Files named '"),
        new(
            "'. Há exatamente um diário por KB; a duplicidade precisa ser resolvida à mão.",
            "'. Hay exactamente un diario por KB; la duplicidad debe ser resuelta a mano.",
            "'. There is exactly one journal per KB; the duplicate must be resolved by hand."),
        new(
            "'. Defaults conservadores em memoria aplicados.",
            "'. Defaults conservadores en memoria aplicados.",
            "'. Conservative in-memory defaults applied."),
        new(
            "Já existe um File '",
            "Ya existe un File '",
            "A File '"),
        new(
            "' que não é do gerador: a Description não é a própria.",
            "' que no es del generador: la Description no es la propia.",
            "' that is not the generator's already exists: the Description is not our own."),
        // B111/F3 P7 (dívida fechada na P8): as violações de schema do envelope, do leitor
        // e do validador. Elas só aparecem com um diário corrompido ou editado à mão, mas
        // quem as encontra numa KB em espanhol ou inglês não escolheu ler português — e é
        // justamente o momento em que a pessoa mais precisa entender o que está escrito.
        // Os caminhos de JSON e os valores de enum ficam como estão: são o que se procura
        // dentro do arquivo. O que passa a mudar de idioma é a frase em volta deles.
        //
        // Ordem importa: as frases inteiras vêm antes dos sufixos genéricos do leitor de
        // campos, e ' ou null.' vem por último, depois de todas as que terminam assim.
        new(
            "Gravação do diário bloqueada: o envelope viola o schema V1. ",
            "Grabación del diario bloqueada: el sobre viola el schema V1. ",
            "Journal write blocked: the envelope violates schema V1. "),
        new(
            "O diário deve ser um objeto JSON.",
            "El diario debe ser un objeto JSON.",
            "The journal must be a JSON object."),
        new(
            "JSON inválido: ",
            "JSON inválido: ",
            "Invalid JSON: "),
        new(
            "schemaVersion desconhecida: esperado ",
            "schemaVersion desconocida: esperado ",
            "unknown schemaVersion: expected "),
        new(
            "journalKind deve ser ",
            "journalKind debe ser ",
            "journalKind must be "),
        new(
            "plan é obrigatório e deve ser um objeto.",
            "plan es obligatorio y debe ser un objeto.",
            "plan is required and must be an object."),
        new(
            "plan.services é obrigatório e deve ser um array.",
            "plan.services es obligatorio y debe ser un array.",
            "plan.services is required and must be an array."),
        new(
            "plan.services só aceita strings não vazias.",
            "plan.services solo acepta strings no vacías.",
            "plan.services accepts only non-empty strings."),
        new(
            "inventory é obrigatório e deve ser um array.",
            "inventory es obligatorio y debe ser un array.",
            "inventory is required and must be an array."),
        new(
            "inventory só aceita objetos.",
            "inventory solo acepta objetos.",
            "inventory accepts only objects."),
        new(
            "inventory[].composite deve ser objeto ou null.",
            "inventory[].composite debe ser objeto o null.",
            "inventory[].composite must be an object or null."),
        new(
            "inventory[].receiptSequences é obrigatório e deve ser um array.",
            "inventory[].receiptSequences es obligatorio y debe ser un array.",
            "inventory[].receiptSequences is required and must be an array."),
        new(
            "inventory[].receiptSequences só aceita inteiros.",
            "inventory[].receiptSequences solo acepta enteros.",
            "inventory[].receiptSequences accepts only integers."),
        new(
            "receipts é obrigatório e deve ser um array.",
            "receipts es obligatorio y debe ser un array.",
            "receipts is required and must be an array."),
        new(
            "receipts só aceita objetos.",
            "receipts solo acepta objetos.",
            "receipts accepts only objects."),
        new(
            "abandonment deve ser objeto ou null.",
            "abandonment debe ser objeto o null.",
            "abandonment must be an object or null."),
        new(
            "operationId e applicationId devem ser distintos.",
            "operationId y applicationId deben ser distintos.",
            "operationId and applicationId must be distinct."),
        new(
            "updatedUtc não pode ser anterior a createdUtc.",
            "updatedUtc no puede ser anterior a createdUtc.",
            "updatedUtc cannot be earlier than createdUtc."),
        new(
            "envelopePhase=Prepared exige operationState=Pending, salvo o abandono explícito.",
            "envelopePhase=Prepared exige operationState=Pending, salvo el abandono explícito.",
            "envelopePhase=Prepared requires operationState=Pending, except for explicit abandonment."),
        new(
            "envelopePhase=Active não admite operationState=Pending.",
            "envelopePhase=Active no admite operationState=Pending.",
            "envelopePhase=Active does not allow operationState=Pending."),
        new(
            "operationState=Pending admite apenas logicalStage NotStarted ou IntentionRecorded.",
            "operationState=Pending admite solo logicalStage NotStarted o IntentionRecorded.",
            "operationState=Pending allows only logicalStage NotStarted or IntentionRecorded."),
        new(
            "operationState=Completed exige logicalStage Completed, Abandoned ou Discarded.",
            "operationState=Completed exige logicalStage Completed, Abandoned o Discarded.",
            "operationState=Completed requires logicalStage Completed, Abandoned, or Discarded."),
        new(
            "operationState=Removed pertence somente a operationKind=Remove.",
            "operationState=Removed pertenece solo a operationKind=Remove.",
            "operationState=Removed belongs only to operationKind=Remove."),
        new(
            "operationState=Removed exige logicalStage=Removed.",
            "operationState=Removed exige logicalStage=Removed.",
            "operationState=Removed requires logicalStage=Removed."),
        new(
            "operationState=Partial em Remove exige logicalStage=RemovalPartial.",
            "operationState=Partial en Remove exige logicalStage=RemovalPartial.",
            "operationState=Partial in Remove requires logicalStage=RemovalPartial."),
        new(
            "operationKind=Recovery autônomo termina em Completed ou OutcomeUnknown.",
            "operationKind=Recovery autónomo termina en Completed o OutcomeUnknown.",
            "a standalone operationKind=Recovery ends in Completed or OutcomeUnknown."),
        new(
            "operationKind=Recovery exige intentKind=Imported.",
            "operationKind=Recovery exige intentKind=Imported.",
            "operationKind=Recovery requires intentKind=Imported."),
        new(
            "logicalStage=Abandoned exige o objeto abandonment.",
            "logicalStage=Abandoned exige el objeto abandonment.",
            "logicalStage=Abandoned requires the abandonment object."),
        new(
            "logicalStage=Discarded exige o objeto abandonment com a disposição de quem encerrou.",
            "logicalStage=Discarded exige el objeto abandonment con la disposición de quien cerró.",
            "logicalStage=Discarded requires the abandonment object with the disposition of whoever closed it."),
        new(
            "o abandono mantém operationState=Completed.",
            "el abandono mantiene operationState=Completed.",
            "abandonment keeps operationState=Completed."),
        new(
            "somente um envelope Prepared pode ser abandonado.",
            "solo un sobre Prepared puede ser abandonado.",
            "only a Prepared envelope can be abandoned."),
        new(
            "o abandono exige journalDurability=Confirmed.",
            "el abandono exige journalDurability=Confirmed.",
            "abandonment requires journalDurability=Confirmed."),
        new(
            "o abandono não admite recibos de gravação de negócio.",
            "el abandono no admite recibos de grabación de negocio.",
            "abandonment does not allow business write receipts."),
        new(
            "abandonment só é válido com logicalStage Abandoned ou Discarded.",
            "abandonment solo es válido con logicalStage Abandoned o Discarded.",
            "abandonment is valid only with logicalStage Abandoned or Discarded."),
        new(
            "o encerramento do registro mantém operationState=Completed.",
            "el cierre del registro mantiene operationState=Completed.",
            "closing the record keeps operationState=Completed."),
        new(
            "somente um envelope Active pode ter o registro encerrado.",
            "solo un sobre Active puede tener el registro cerrado.",
            "only an Active envelope can have its record closed."),
        new(
            "o encerramento do registro exige journalDurability=Confirmed.",
            "el cierre del registro exige journalDurability=Confirmed.",
            "closing the record requires journalDurability=Confirmed."),
        new(
            "operationState Partial ou OutcomeUnknown exige blockReason.",
            "operationState Partial o OutcomeUnknown exige blockReason.",
            "operationState Partial or OutcomeUnknown requires blockReason."),
        new(
            "blockReason só é persistido com operationState Partial ou OutcomeUnknown.",
            "blockReason solo se persiste con operationState Partial o OutcomeUnknown.",
            "blockReason is persisted only with operationState Partial or OutcomeUnknown."),
        new(
            "blockReason=RetryBudgetExhausted pertence ao orçamento de passadas do Remove.",
            "blockReason=RetryBudgetExhausted pertenece al presupuesto de pasadas del Remove.",
            "blockReason=RetryBudgetExhausted belongs to the Remove pass budget."),
        new(
            "blockReason=UserAborted exige operationState=Partial.",
            "blockReason=UserAborted exige operationState=Partial.",
            "blockReason=UserAborted requires operationState=Partial."),
        new(
            "plan.plannedApiGuid não pode ser o GUID vazio.",
            "plan.plannedApiGuid no puede ser el GUID vacío.",
            "plan.plannedApiGuid cannot be the empty GUID."),
        new(
            "plan.contractHash é obrigatório em Apply e Sync.",
            "plan.contractHash es obligatorio en Apply y Sync.",
            "plan.contractHash is required in Apply and Sync."),
        new(
            "plan de Remove exige o inventário completo dos alvos.",
            "el plan de Remove exige el inventario completo de los objetivos.",
            "the Remove plan requires the complete inventory of targets."),
        new(
            "plan.contractHash só pode ser nulo em Remove sobre metadata legada importada.",
            "plan.contractHash solo puede ser nulo en Remove sobre metadatos heredados importados.",
            "plan.contractHash can be null only in a Remove over imported legacy metadata."),
        new(
            "plan.plannedApiGuid é obrigatório quando o inventário de Remove contém o API Object.",
            "plan.plannedApiGuid es obligatorio cuando el inventario de Remove contiene el API Object.",
            "plan.plannedApiGuid is required when the Remove inventory contains the API Object."),
        new(
            "plan.contractHash não existe em MetadataRecovery: a recuperação não reconstrói contrato.",
            "plan.contractHash no existe en MetadataRecovery: la recuperación no reconstruye contrato.",
            "plan.contractHash does not exist in MetadataRecovery: recovery does not rebuild a contract."),
        new(
            "plan.services não admite entradas vazias.",
            "plan.services no admite entradas vacías.",
            "plan.services does not allow empty entries."),
        new(
            "as flags de geração não pertencem ao plano de ",
            "las flags de generación no pertenecen al plan de ",
            "the generation flags do not belong to the plan of "),
        new(
            "receipts[].sequence deve ser único dentro da operação: ",
            "receipts[].sequence debe ser único dentro de la operación: ",
            "receipts[].sequence must be unique within the operation: "),
        new(
            "receipts[].sequence deve ser inteiro positivo.",
            "receipts[].sequence debe ser entero positivo.",
            "receipts[].sequence must be a positive integer."),
        new(
            "receipts deve ser monotônico dentro da operação.",
            "receipts debe ser monotónico dentro de la operación.",
            "receipts must be monotonic within the operation."),
        new(
            "receipts[].attempt deve ser inteiro positivo.",
            "receipts[].attempt debe ser entero positivo.",
            "receipts[].attempt must be a positive integer."),
        new(
            "receipts[].retryOfSequence deve apontar para um recibo anterior.",
            "receipts[].retryOfSequence debe apuntar a un recibo anterior.",
            "receipts[].retryOfSequence must point to an earlier receipt."),
        new(
            "receipts[].retryOfSequence referencia um recibo inexistente: ",
            "receipts[].retryOfSequence referencia un recibo inexistente: ",
            "receipts[].retryOfSequence references a nonexistent receipt: "),
        new(
            "receipts[].retryEligible=true exige Delete, Failed, Present e StillPresentAfterDelete.",
            "receipts[].retryEligible=true exige Delete, Failed, Present y StillPresentAfterDelete.",
            "receipts[].retryEligible=true requires Delete, Failed, Present, and StillPresentAfterDelete."),
        new(
            "receipts[].retryableReason só existe com retryEligible=true.",
            "receipts[].retryableReason solo existe con retryEligible=true.",
            "receipts[].retryableReason exists only with retryEligible=true."),
        new(
            " não pertence ao domínio de operationKind=",
            " no pertenece al dominio de operationKind=",
            " does not belong to the domain of operationKind="),
        new(
            "a Transaction nunca entra na fila destrutiva.",
            "la Transaction nunca entra en la cola destructiva.",
            "the Transaction never enters the destructive queue."),
        new(
            "inventory[].receiptSequences referencia um recibo inexistente: ",
            "inventory[].receiptSequences referencia un recibo inexistente: ",
            "inventory[].receiptSequences references a nonexistent receipt: "),
        new(
            "inventory repete o mesmo alvo: ",
            "inventory repite el mismo objetivo: ",
            "inventory repeats the same target: "),
        new(
            "identityKind=Guid exige guid.",
            "identityKind=Guid exige guid.",
            "identityKind=Guid requires guid."),
        new(
            "identityKind=FileId exige fileId inteiro positivo.",
            "identityKind=FileId exige fileId entero positivo.",
            "identityKind=FileId requires a positive integer fileId."),
        new(
            "identityKind=FileId exige expectedHash.",
            "identityKind=FileId exige expectedHash.",
            "identityKind=FileId requires expectedHash."),
        new(
            "identityKind=Composite exige a identidade histórica completa.",
            "identityKind=Composite exige la identidad histórica completa.",
            "identityKind=Composite requires the complete historical identity."),
        new(
            "identityKind=Folder exige emptyConfirmed=true para ser removido.",
            "identityKind=Folder exige emptyConfirmed=true para ser removido.",
            "identityKind=Folder requires emptyConfirmed=true to be removed."),
        new(
            "identityKind=Folder exige posse própria validada.",
            "identityKind=Folder exige pertenencia propia validada.",
            "identityKind=Folder requires validated own ownership."),
        new(
            "identityKind=None só é permitido em item Preserve.",
            "identityKind=None solo se permite en ítem Preserve.",
            "identityKind=None is allowed only on a Preserve item."),
        new(
            "metadataSchemaVersion é obrigatório quando a operação envolve metadata.",
            "metadataSchemaVersion es obligatorio cuando la operación involucra metadatos.",
            "metadataSchemaVersion is required when the operation involves metadata."),
        new(
            "metadataSchemaVersion desconhecida: ",
            "metadataSchemaVersion desconocida: ",
            "unknown metadataSchemaVersion: "),
        new(
            " é obrigatório e deve ser uma string não vazia.",
            " es obligatorio y debe ser una string no vacía.",
            " is required and must be a non-empty string."),
        new(
            " deve ser uma string não vazia ou null.",
            " debe ser una string no vacía o null.",
            " must be a non-empty string or null."),
        new(
            " é obrigatório e deve ser um timestamp UTC.",
            " es obligatorio y debe ser un timestamp UTC.",
            " is required and must be a UTC timestamp."),
        new(
            " deve seguir yyyy-MM-ddTHH:mm:ss.fffZ.",
            " debe seguir yyyy-MM-ddTHH:mm:ss.fffZ.",
            " must follow yyyy-MM-ddTHH:mm:ss.fffZ."),
        new(
            " é obrigatório e deve ser inteiro.",
            " es obligatorio y debe ser entero.",
            " is required and must be an integer."),
        new(
            " deve ser inteiro ou null.",
            " debe ser entero o null.",
            " must be an integer or null."),
        new(
            " é obrigatório e deve ser booleano.",
            " es obligatorio y debe ser booleano.",
            " is required and must be a boolean."),
        new(
            " deve ser booleano ou null.",
            " debe ser booleano o null.",
            " must be a boolean or null."),
        new(
            " é obrigatório e deve ser um GUID.",
            " es obligatorio y debe ser un GUID.",
            " is required and must be a GUID."),
        new(
            " deve ser um GUID ou null.",
            " debe ser un GUID o null.",
            " must be a GUID or null."),
        new(
            " é obrigatório e deve ser um valor conhecido de ",
            " es obligatorio y debe ser un valor conocido de ",
            " is required and must be a known value of "),
        new(
            " deve ser um valor conhecido de ",
            " debe ser un valor conocido de ",
            " must be a known value of "),
        new(
            " ou null.",
            " o null.",
            " or null."),
        new(
            ", encontrado ",
            ", encontrado ",
            ", found "),
        new(
            " é obrigatório.",
            " es obligatorio.",
            " is required."),
        // B111/F3 P7 (dívida fechada na P8): as recusas de transição do envelope e o que a
        // sessão publica. Estas escaparam de três rodadas porque **não nascem como mensagem
        // de tela**: nascem como `InvalidOperationException` num método de checkpoint, e só
        // chegam ao diálogo concatenadas a um prefixo que já era trilíngue — o resultado era
        // meia frase em cada idioma, que parece defeito de software.
        //
        // Os nomes de transição entram COM as aspas simples que sempre os cercam. `abandono`
        // solto recortaria qualquer frase do catálogo que fale de abandono, e há várias.
        new(
            "Para registrar o API Object confirmado o envelope precisa estar Active/Running. Estado atual: ",
            "Para registrar el API Object confirmado el sobre debe estar Active/Running. Estado actual: ",
            "To record the confirmed API Object the envelope must be Active/Running. Current state: "),
        new(
            "Para registrar resultado indeterminado do API Object o envelope precisa estar Active/Running. Estado atual: ",
            "Para registrar resultado indeterminado del API Object el sobre debe estar Active/Running. Estado actual: ",
            "To record an undetermined API Object result the envelope must be Active/Running. Current state: "),
        new(
            "Para registrar metadata recuperada o envelope precisa estar Active/Running. Estado atual: ",
            "Para registrar metadatos recuperados el sobre debe estar Active/Running. Estado actual: ",
            "To record recovered metadata the envelope must be Active/Running. Current state: "),
        new(
            "Para registrar uma passada de remoção o envelope precisa estar Active/Running. Estado atual: ",
            "Para registrar una pasada de eliminación el sobre debe estar Active/Running. Estado actual: ",
            "To record a removal pass the envelope must be Active/Running. Current state: "),
        new(
            "Para concluir a operação o envelope precisa estar Active/Running. Estado atual: ",
            "Para concluir la operación el sobre debe estar Active/Running. Estado actual: ",
            "To complete the operation the envelope must be Active/Running. Current state: "),
        new(
            "Para interromper a operação o envelope precisa estar Active/Running. Estado atual: ",
            "Para interrumpir la operación el sobre debe estar Active/Running. Estado actual: ",
            "To interrupt the operation the envelope must be Active/Running. Current state: "),
        new(
            "Só um envelope Prepared/Pending pode ser promovido a Active. Estado atual: ",
            "Solo un sobre Prepared/Pending puede ser promovido a Active. Estado actual: ",
            "Only a Prepared/Pending envelope can be promoted to Active. Current state: "),
        new(
            "Só um envelope Prepared/Pending pode ser abandonado. Estado atual: ",
            "Solo un sobre Prepared/Pending puede ser abandonado. Estado actual: ",
            "Only a Prepared/Pending envelope can be abandoned. Current state: "),
        new(
            "Só um envelope Active pode ter o registro encerrado; um Prepared é abandonado. Estado atual: ",
            "Solo un sobre Active puede tener el registro cerrado; un Prepared se abandona. Estado actual: ",
            "Only an Active envelope can have its record closed; a Prepared one is abandoned. Current state: "),
        new(
            "Só um envelope Active pode ser reconciliado como removido. Estado atual: ",
            "Solo un sobre Active puede ser reconciliado como removido. Estado actual: ",
            "Only an Active envelope can be reconciled as removed. Current state: "),
        new(
            "Só uma remoção Active/Partial pode ser retomada. Estado atual: ",
            "Solo una eliminación Active/Partial puede ser retomada. Estado actual: ",
            "Only an Active/Partial removal can be resumed. Current state: "),
        new(
            "ApiPhysicallySaved pertence a Apply e Sync, não a ",
            "ApiPhysicallySaved pertenece a Apply y Sync, no a ",
            "ApiPhysicallySaved belongs to Apply and Sync, not to "),
        new(
            "MetadataRecovered pertence ao Recovery autônomo, não a ",
            "MetadataRecovered pertenece al Recovery autónomo, no a ",
            "MetadataRecovered belongs to the standalone Recovery, not to "),
        new(
            "Passadas de remoção pertencem ao Remove, não a ",
            "Las pasadas de eliminación pertenecen al Remove, no a ",
            "Removal passes belong to Remove, not to "),
        new(
            "A retomada de passadas pertence ao Remove, não a ",
            "La reanudación de pasadas pertenece al Remove, no a ",
            "Resuming passes belongs to Remove, not to "),
        new(
            "A reconciliação para Removed pertence ao Remove, não a ",
            "La reconciliación a Removed pertenece al Remove, no a ",
            "Reconciling to Removed belongs to Remove, not to "),
        new(
            "A reconciliação para Removed parte de Partial ou Running, não de ",
            "La reconciliación a Removed parte de Partial o Running, no de ",
            "Reconciling to Removed starts from Partial or Running, not from "),
        new(
            "O encerramento do registro parte de Partial ou Running, não de ",
            "El cierre del registro parte de Partial o Running, no de ",
            "Closing the record starts from Partial or Running, not from "),
        new(
            "Uma interrupção termina em Partial ou OutcomeUnknown, não em ",
            "Una interrupción termina en Partial o OutcomeUnknown, no en ",
            "An interruption ends in Partial or OutcomeUnknown, not in "),
        new(
            "A retomada não cobre o motivo registrado: ",
            "La reanudación no cubre el motivo registrado: ",
            "Resuming does not cover the recorded reason: "),
        new(
            "Encerrar um registro cuja durabilidade não foi confirmada esconderia justamente o que não se sabe.",
            "Cerrar un registro cuya durabilidad no fue confirmada escondería justamente lo que no se sabe.",
            "Closing a record whose durability was not confirmed would hide precisely what is not known."),
        new(
            "A continuação do envelope preparado ainda não tem serviço que a execute.",
            "La continuación del sobre preparado todavía no tiene servicio que la ejecute.",
            "The continuation of the prepared envelope has no service to execute it yet."),
        new(
            "Transição '",
            "Transición '",
            "Transition '"),
        new(
            "' produziu um envelope inválido. ",
            "' produjo un sobre inválido. ",
            "' produced an invalid envelope. "),
        new(
            "'CP3 passada de remoção'",
            "'CP3 pasada de eliminación'",
            "'CP3 removal pass'"),
        new(
            "'CP4 interrupção'",
            "'CP4 interrupción'",
            "'CP4 interruption'"),
        new(
            "'retomada de remoção'",
            "'reanudación de eliminación'",
            "'removal resumption'"),
        new(
            "'reconciliação para Removed'",
            "'reconciliación a Removed'",
            "'reconciliation to Removed'"),
        new(
            "'abandono'",
            "'abandono'",
            "'abandonment'"),
        new(
            "'encerramento do registro'",
            "'cierre del registro'",
            "'record closing'"),
        new(
            "'API Object confirmado'",
            "'API Object confirmado'",
            "'confirmed API Object'"),
        new(
            "'resultado indeterminado do API Object'",
            "'resultado indeterminado del API Object'",
            "'undetermined API Object result'"),
        new(
            "'metadata recuperada'",
            "'metadatos recuperados'",
            "'recovered metadata'"),
        new(
            "'passada de remoção'",
            "'pasada de eliminación'",
            "'removal pass'"),
        new(
            "'conclusão'",
            "'conclusión'",
            "'completion'"),
        new(
            "'interrupção (",
            "'interrupción (",
            "'interruption ("),
        new(
            "O envelope Prepared do diário não pôde ser confirmado: ",
            "El sobre Prepared del diario no pudo ser confirmado: ",
            "The Prepared journal envelope could not be confirmed: "),
        new(
            "A promoção do diário a Active não pôde ser confirmada: ",
            "La promoción del diario a Active no pudo ser confirmada: ",
            "Promoting the journal to Active could not be confirmed: "),
        new(
            "A identidade da API mudou durante a operação: o diário registra '",
            "La identidad de la API cambió durante la operación: el diario registra '",
            "The API identity changed during the operation: the journal records '"),
        new(
            "' e o pipeline apresentou '",
            "' y el pipeline presentó '",
            "' and the pipeline presented '"),
        new(
            "' não foi gravado: o diário já está bloqueado. ",
            "' no fue grabado: el diario ya está bloqueado. ",
            "' was not written: the journal is already blocked. "),
        new(
            "A transição '",
            "La transición '",
            "The transition '"),
        new(
            "' não é válida no estado atual: ",
            "' no es válida en el estado actual: ",
            "' is not valid in the current state: "),
        new(
            "O checkpoint '",
            "El checkpoint '",
            "Checkpoint '"),
        new(
            "' falhou ao ser gravado: ",
            "' falló al ser grabado: ",
            "' failed to be written: "),
        new(
            " A operação seguiu; o diário ficou bloqueado e o último snapshot durável foi preservado.",
            " La operación siguió; el diario quedó bloqueado y el último snapshot durable fue preservado.",
            " The operation continued; the journal was blocked and the last durable snapshot was preserved."),
        new(
            "' não pôde ser confirmado: ",
            "' no pudo ser confirmado: ",
            "' could not be confirmed: "),
        new(
            " O último snapshot durável foi preservado e a operação não continua às cegas.",
            " El último snapshot durable fue preservado y la operación no continúa a ciegas.",
            " The last durable snapshot was preserved and the operation does not continue blindly."),
        new(
            "Custo do diário: Checkpoints=",
            "Costo del diario: Checkpoints=",
            "Journal cost: Checkpoints="),
        new(
            ", Estado=",
            ", Estado=",
            ", State="),
        new(
            "O checkpoint da passada ",
            "El checkpoint de la pasada ",
            "The checkpoint of pass "),
        new(
            " não pôde ser confirmado no diário; a remoção parou para não continuar sem estado durável.",
            " no pudo ser confirmado en el diario; la eliminación se detuvo para no continuar sin estado durable.",
            " could not be confirmed in the journal; the removal stopped rather than continue without durable state."),
        new(
            "Remocao bloqueada: File de metadata '",
            "Eliminación bloqueada: File de metadatos '",
            "Removal blocked: metadata File '"),
        new(
            "' nao foi encontrado.",
            "' no fue encontrado.",
            "' was not found."),
        new(
            "Remocao bloqueada: foram encontrados ",
            "Eliminación bloqueada: se encontraron ",
            "Removal blocked: found "),
        new(
            "Remocao bloqueada: File '",
            "Eliminación bloqueada: File '",
            "Removal blocked: File '"),
        new(
            "' nao e metadata propria da extensao.",
            "' no es metadatos propios de la extensión.",
            "' is not metadata owned by the extension."),
        new(
            "' nao possui JSON persistido.",
            "' no posee JSON persistido.",
            "' has no persisted JSON."),
        new(
            "' possui JSON invalido.",
            "' posee JSON inválido.",
            "' has invalid JSON."),
        new(
            "Remocao bloqueada: ",
            "Eliminación bloqueada: ",
            "Removal blocked: "),
        new(
            "O alvo ainda existe após Delete.",
            "El objetivo todavía existe después del Delete.",
            "The target still exists after Delete."),
        new(
            "O alvo não foi reencontrado após Delete.",
            "El objetivo no fue reencontrado después del Delete.",
            "The target was not found again after Delete."),
        // B111/F3 P8: o aborto e o aviso de remoção parcial apontavam para Remover / Wizard /
        // Sync, que deixaram de ser a saída quando o diário passou a bloquear a reentrada. A
        // saída é o comando de recuperação — e, no caso da remoção, repetir do zero bloqueia
        // em `TargetAbsentBeforeDelete`, que é o contrato da seção 4.3.
        new(
            "Operação abortada pelo usuário. O objeto em curso foi concluído; a KB pode ter ficado inconsistente. Use o comando 'Recuperar operação interrompida' para ver o que ficou registrado e escolher a saída.",
            "Operación abortada por el usuario. El objeto en curso fue concluido; la KB puede haber quedado inconsistente. Use el comando 'Recuperar operación interrumpida' para ver lo que quedó registrado y elegir la salida.",
            "Operation aborted by the user. The object in progress was completed; the KB may have been left inconsistent. Use the 'Recover interrupted operation' command to see what was recorded and choose how to proceed."),
        new(
            " objeto(s) já foram excluídos e estão listados como removidos. A API ficou incompleta; use o comando 'Recuperar operação interrompida' para retomar a fila no mesmo registro. Repetir a remoção do zero bloqueia, porque os objetos já apagados não estão mais na KB.",
            " objeto(s) ya fueron eliminados y están listados como eliminados. La API quedó incompleta; use el comando 'Recuperar operación interrumpida' para retomar la cola en el mismo registro. Repetir la eliminación desde cero bloquea, porque los objetos ya borrados no están más en la KB.",
            " object(s) were already deleted and are listed as removed. The API was left incomplete; use the 'Recover interrupted operation' command to resume the queue in the same record. Starting the removal over blocks, because the objects already deleted are no longer in the KB."),
        new(
            "Remoção parcial: ",
            "Eliminación parcial: ",
            "Partial removal: "),
        // O relatório final da recuperação: sem estes dois, o verbo caía no default «API gerada».
        new(
            "Operação recuperada",
            "Operación recuperada",
            "Operation recovered"),
        new(
            "Recuperação interrompida.",
            "Recuperación interrumpida.",
            "Recovery interrupted."),
        new(
            "Operação ",
            "Operación ",
            "Operation "),
        new(
            " sobre '",
            " sobre '",
            " over '"),
        new(
            "': estado ",
            "': estado ",
            "': state "),
        new(
            ", envelope ",
            ", sobre ",
            ", envelope "),
        new(
            ", durabilidade ",
            ", durabilidad ",
            ", durability "),
        new(
            ", atualizado em ",
            ", actualizado en ",
            ", updated at "),
        new(
            "Motivo registrado no envelope: ",
            "Motivo registrado en el sobre: ",
            "Reason recorded in the envelope: "),
        new(
            "Próxima etapa apurada: ",
            "Próxima etapa determinada: ",
            "Next step determined: "),
        new(
            "A KB não tem diário de operação: nenhuma operação desta ferramenta ficou pendente aqui.",
            "La KB no tiene diario de operación: ninguna operación de esta herramienta quedó pendiente aquí.",
            "The KB has no operation journal: no operation from this tool was left pending here."),
        new(
            "plan.plannedApiGuid é obrigatório em Apply e Sync a partir do estágio ",
            "plan.plannedApiGuid es obligatorio en Apply y Sync a partir de la etapa ",
            "plan.plannedApiGuid is required in Apply and Sync from stage "),
        // B111/F3 P7 (dívida fechada na P8): o que a primeira sonda não viu. Ela exigia acento
        // ou palavra-marcador para considerar um literal, e estas frases são de uma leva antiga,
        // escritas em ASCII sem acento — `nao e proprio da extensao`. Uma segunda sonda, sem esse
        // filtro, achou as sete causas que o preflight da remoção cola em `Remocao bloqueada: {x}`
        // e mais algumas. A lição fica: heurística de idioma por acento não encontra texto antigo.
        new(
            "API Object ambiguo '",
            "API Object ambiguo '",
            "ambiguous API Object '"),
        new(
            "' nao corresponde ao Guid registrado",
            "' no corresponde al Guid registrado",
            "' does not match the recorded Guid"),
        new(
            "Procedure ambigua '",
            "Procedure ambigua '",
            "ambiguous Procedure '"),
        new(
            "' nao e propria da extensao",
            "' no es propia de la extensión",
            "' is not owned by the extension"),
        new(
            "tentativa de apagar SDT compartilhado '",
            "intento de borrar SDT compartido '",
            "attempt to delete shared SDT '"),
        new(
            "SDT ambiguo '",
            "SDT ambiguo '",
            "ambiguous SDT '"),
        new(
            "' nao e proprio da extensao",
            "' no es propio de la extensión",
            "' is not owned by the extension"),
        new(
            ". O estado da KB mudou apos o preflight; interrompendo para evitar mais exclusoes.",
            ". El estado de la KB cambió después del preflight; interrumpiendo para evitar más exclusiones.",
            ". The KB state changed after the preflight; stopping to avoid further deletions."),
        new(
            "Procedure ausente antes do Delete.",
            "Procedure ausente antes del Delete.",
            "Procedure absent before Delete."),
        new(
            "API Object ausente antes do Delete.",
            "API Object ausente antes del Delete.",
            "API Object absent before Delete."),
        new(
            "SDT ausente antes do Delete.",
            "SDT ausente antes del Delete.",
            "SDT absent before Delete."),
        new(
            "File de metadata ausente antes do Delete.",
            "File de metadatos ausente antes del Delete.",
            "Metadata File absent before Delete."),
        new(
            "A abertura do diário falhou: ",
            "La apertura del diario falló: ",
            "Opening the journal failed: "),
        new(
            "plan.planKind incompatível com operationKind=",
            "plan.planKind incompatible con operationKind=",
            "plan.planKind incompatible with operationKind="),
        new(
            "Metadata de remoção incompatível em '",
            "Metadatos de eliminación incompatibles en '",
            "Incompatible removal metadata in '"),
        new(
            "Membro 'schemaVersion' incompativel",
            "Miembro 'schemaVersion' incompatible",
            "Member 'schemaVersion' is incompatible"),
        new(
            " ou ausente (legado), atual=",
            " o ausente (heredado), actual=",
            " or absent (legacy), current="),
        new(
            "V1, V2 ou V3",
            "V1, V2 o V3",
            "V1, V2 or V3"),
        new(
            ": esperado ",
            ": esperado ",
            ": expected "),
        // B111/F3 P8 (cenário 8): as recusas do plano de remoção sobre metadata legada ou
        // adulterada. Nenhuma estava no catálogo — saíam em português em qualquer KB —, e a
        // frase de saída é comum às três.
        new(
            "Metadata de remoção inválida: campo '",
            "Metadatos de eliminación inválidos: campo '",
            "Invalid removal metadata: field '"),
        new(
            "' ausente.",
            "' ausente.",
            "' is missing."),
        new(
            " A remoção não apaga nada sem o inventário completo: corrija o File de metadata ou, para regerar a API sobre o que restou na KB, apague o File de metadata e o API Object e execute o Wizard de novo.",
            " La eliminación no borra nada sin el inventario completo: corrija el File de metadatos o, para regenerar la API sobre lo que quedó en la KB, borre el File de metadatos y el API Object y ejecute el Wizard de nuevo.",
            " The removal deletes nothing without the complete inventory: fix the metadata File or, to regenerate the API over what is left in the KB, delete the metadata File and the API Object and run the Wizard again."),
        new(
            "Metadata hierárquica com levels ilegível; a remoção não usa fallback flat. Corrija a metadata ou regenere a API.",
            "Metadatos jerárquicos con levels ilegible; la eliminación no usa fallback flat. Corrija los metadatos o regenere la API.",
            "Hierarchical metadata with unreadable levels; the removal does not fall back to the flat inventory. Fix the metadata or regenerate the API."),
        new(
            "levels.levelName é obrigatório.",
            "levels.levelName es obligatorio.",
            "levels.levelName is required."),
        new(
            "', encontrado '",
            "', encontrado '",
            "', found '"),
        // O File do diário sem módulo: o rótulo aparece na linha de abertura, na Output.
        new("<sem módulo>", "<sin módulo>", "<no module>"),
        new("A operação Apply", "La operación Apply", "The Apply operation"),
        new("A operação Sync", "La operación Sync", "The Sync operation"),
        new("A operação Remove", "La operación Remove", "The Remove operation"),
        new("A operação Recovery", "La operación Recovery", "The Recovery operation"),
        new("Gravação de metadata B060 bloqueada: o File '", "Grabación de metadatos B060 bloqueada: el File '", "B060 metadata write blocked: the File '"),
        // A mesma orientação, no ponto em que o Wizard desliga a etapa de metadata — antes de
        // qualquer gravação, que é onde ela é realmente lida.
        new(
            "a metadata registra um API Object que não está mais na KB. Para regerar a API a partir do que restou, apague o File '",
            "los metadatos registran un API Object que ya no está en la KB. Para regenerar la API a partir de lo que quedó, borre el File '",
            "the metadata records an API Object that is no longer in the KB. To regenerate the API from what is left, delete the File '"),
        new(
            "a metadata registra um API Object diferente do que está na KB com esse nome",
            "los metadatos registran un API Object distinto del que está en la KB con ese nombre",
            "the metadata records an API Object different from the one in the KB with that name"),
        new(
            "a metadata da API não está na KB, e sem ela a posse do API Object existente não pode ser confirmada",
            "los metadatos de la API no están en la KB, y sin ellos la pertenencia del API Object existente no puede confirmarse",
            "the API metadata is not in the KB, and without it the ownership of the existing API Object cannot be confirmed"),
        new(
            ". Para regerar a API a partir do que restou, apague o API Object '",
            ". Para regenerar la API a partir de lo que quedó, borre el API Object '",
            ". To regenerate the API from what is left, delete the API Object '"),
        new(
            "' — os dois, porque um sem o outro apenas troca este bloqueio pelo seguinte — e execute o Wizard de novo:",
            "' — los dos, porque uno sin el otro solo cambia este bloqueo por el siguiente — y ejecute el Wizard de nuevo:",
            "' — both, because one without the other only swaps this block for the next one — and run the Wizard again:"),
        new(
            "' e execute o Wizard de novo: os SDTs e as Procedures existentes são reencontrados, e o API Object e a metadata são recriados. Paginação, ordenação e campos obrigatórios voltam aos padrões das preferências, porque só existiam na metadata apagada.",
            "' y ejecute el Wizard de nuevo: los SDTs y los Procedures existentes se reencuentran, y el API Object y los metadatos se recrean. Paginación, ordenación y campos obligatorios vuelven a los valores predeterminados de las preferencias, porque solo existían en los metadatos borrados.",
            "' and run the Wizard again: existing SDTs and Procedures are rediscovered, and the API Object and the metadata are recreated. Pagination, ordering, and required fields fall back to the preference defaults, because they only existed in the deleted metadata."),
        new(
            " os SDTs e as Procedures existentes são reencontrados, e o API Object e a metadata são recriados. Paginação, ordenação e campos obrigatórios voltam aos padrões das preferências, porque só existiam na metadata apagada.",
            " los SDTs y los Procedures existentes se reencuentran, y el API Object y los metadatos se recrean. Paginación, ordenación y campos obligatorios vuelven a los valores predeterminados de las preferencias, porque solo existían en los metadatos borrados.",
            " existing SDTs and Procedures are rediscovered, and the API Object and the metadata are recreated. Pagination, ordering, and required fields fall back to the preference defaults, because they only existed in the deleted metadata."),
        new("Causa='", "Causa='", "Cause='"),
        new(" de metadata", " de metadatos", " metadata"),
        new("API gerada com avisos.", "API generada con advertencias.", "API generated with warnings."),
        new("Relatório final:", "Informe final:", "Final report:"),
        new("Relatorio final:", "Informe final:", "Final report:"),
        new("Operação='", "Operación='", "Operation='"),
        new("Resultado='", "Resultado='", "Outcome='"),
        new("Criados=", "Creados=", "Created="),
        new("Atualizados=", "Actualizados=", "Updated="),
        new("Removidos=", "Eliminados=", "Removed="),
        new("Bloqueados=", "Bloqueados=", "Blocked="),
        new("Avisos=", "Advertencias=", "Warnings="),
        new("DuraçãoMs=", "DuraciónMs=", "DurationMs="),
        new("Título='", "Título='", "Headline='"),
        new("Operação:", "Operación:", "Operation:"),
        new("Operation='", "Operación='", "Operation='"),
        new("Outcome='", "Resultado='", "Outcome='"),
        new("Created=", "Creados=", "Created="),
        new("Updated=", "Actualizados=", "Updated="),
        new("Deleted=", "Eliminados=", "Deleted="),
        new("Blocked=", "Bloqueados=", "Blocked="),
        new("Warnings=", "Advertencias=", "Warnings="),
        new("DurationMs=", "DuraciónMs=", "DurationMs="),
        new("Headline='", "Titular='", "Headline='"),
        new("Tempo:", "Tiempo:", "Time:"),
        new("Criados:", "Creados:", "Created:"),
        new("Criados (", "Creados (", "Created ("),
        new("Atualizados:", "Actualizados:", "Updated:"),
        new("Atualizados (", "Actualizados (", "Updated ("),
        new("Removidos:", "Eliminados:", "Removed:"),
        new("Removidos (", "Eliminados (", "Removed ("),
        new("Bloqueados:", "Bloqueados:", "Blocked:"),
        new("Bloqueados (", "Bloqueados (", "Blocked ("),
        new("Avisos:", "Advertencias:", "Warnings:"),
        new("Avisos (", "Advertencias (", "Warnings ("),
        new("(nenhum)", "(ninguno)", "(none)"),
        new("(nenhuma)", "(ninguna)", "(none)"),
        new("com sucesso.", "con éxito.", "successfully."),
        new("com avisos.", "con advertencias.", "with warnings."),
        new("Remocao interrompida.", "Eliminación interrumpida.", "Removal interrupted."),
        new("Remoção interrompida.", "Eliminación interrumpida.", "Removal interrupted."),
        new("Sincronizacao interrompida.", "Sincronización interrumpida.", "Synchronization interrupted."),
        new("Sincronização interrompida.", "Sincronización interrumpida.", "Synchronization interrupted."),
        new("Geracao interrompida.", "Generación interrumpida.", "Generation interrupted."),
        new("Geração interrompida.", "Generación interrumpida.", "Generation interrupted."),
        new("Nenhuma Knowledge Base ativa foi encontrada.", "No se encontró ninguna Knowledge Base activa.", "No active Knowledge Base was found."),
        new("Abra uma KB e execute o comando novamente.", "Abra una KB y ejecute el comando nuevamente.", "Open a KB and run the command again."),
        new("Nenhuma alteração foi feita na KB.", "No se realizaron cambios en la KB.", "No changes were made to the KB."),
        new("Nenhuma alteracao foi feita na KB.", "No se realizaron cambios en la KB.", "No changes were made to the KB."),
        new("Nenhuma alteração foi feita.", "No se realizaron cambios.", "No changes were made."),
        new("Nenhuma alteracao foi feita.", "No se realizaron cambios.", "No changes were made."),
        new("Nenhuma outra alteração será feita na KB.", "No se realizarán otros cambios en la KB.", "No other changes will be made to the KB."),
        new("Nenhuma outra alteracao sera feita na KB.", "No se realizarán otros cambios en la KB.", "No other changes will be made to the KB."),
        new("Nenhuma outra alteração será feita.", "No se realizarán otros cambios.", "No other changes will be made."),
        new("Nenhuma outra alteracao sera feita.", "No se realizarán otros cambios.", "No other changes will be made."),
        new("nenhuma alteração foi feita.", "no se realizaron cambios.", "no changes were made."),
        new("nenhuma alteracao foi feita.", "no se realizaron cambios.", "no changes were made."),
        new("Configuração de preferências do wizard cancelada.", "Se canceló la configuración de preferencias del wizard.", "Wizard preference configuration was canceled."),
        new("Configuracao de preferencias do wizard cancelada.", "Se canceló la configuración de preferencias del wizard.", "Wizard preference configuration was canceled."),
        new("Preferências do wizard gravadas na KB ativa:", "Preferencias del wizard guardadas en la KB activa:", "Wizard preferences saved to the active KB:"),
        new("Preferencias do wizard gravadas na KB ativa:", "Preferencias del wizard guardadas en la KB activa:", "Wizard preferences saved to the active KB:"),
        new("O próximo wizard aplicará esses defaults quando a etapa estiver habilitada pelo estado da KB.", "El próximo wizard aplicará estos valores predeterminados cuando la etapa esté habilitada por el estado de la KB.", "The next wizard will apply these defaults when the stage is enabled by the KB state."),
        new("O proximo wizard aplicara esses defaults quando a etapa estiver habilitada pelo estado da KB.", "El próximo wizard aplicará estos valores predeterminados cuando la etapa esté habilitada por el estado de la KB.", "The next wizard will apply these defaults when the stage is enabled by the KB state."),
        new("Gravação de preferências bloqueada ou falhou antes de concluir:", "El guardado de preferencias fue bloqueado o falló antes de finalizar:", "Saving preferences was blocked or failed before completion:"),
        new("Gravacao de preferencias bloqueada ou falhou antes de concluir:", "El guardado de preferencias fue bloqueado o falló antes de finalizar:", "Saving preferences was blocked or failed before completion:"),
        new("Nenhuma Transaction elegível foi encontrada na Knowledge Base ativa.", "No se encontró ninguna Transaction elegible en la Knowledge Base activa.", "No eligible Transaction was found in the active Knowledge Base."),
        new("Transactions elegíveis encontradas:", "Transactions elegibles encontradas:", "Eligible Transactions found:"),
        new("Transaction elegível:", "Transaction elegible:", "Eligible Transaction:"),
        new("O diálogo público de seleção não está disponível nesta IDE.", "El diálogo público de selección no está disponible en esta IDE.", "The public selection dialog is not available in this IDE."),
        new("Nenhuma Transaction foi selecionada.", "No se seleccionó ninguna Transaction.", "No Transaction was selected."),
        new("A seleção retornada não é uma Transaction.", "La selección devuelta no es una Transaction.", "The returned selection is not a Transaction."),
        new("Nenhuma escolha foi mantida.", "No se conservó ninguna selección.", "No selection was kept."),
        new("A Transaction selecionada não possui módulo disponível:", "La Transaction seleccionada no tiene un módulo disponible:", "The selected Transaction has no available module:"),
        new("Transaction selecionada:", "Transaction seleccionada:", "Selected Transaction:"),
        new("Módulo da Transaction:", "Módulo de la Transaction:", "Transaction module:"),
        new("Nenhuma Transaction selecionada em memória.", "No se seleccionó ninguna Transaction en memoria.", "No Transaction is selected in memory."),
        new("Nenhuma Transaction selecionada em memoria.", "No se seleccionó ninguna Transaction en memoria.", "No Transaction is selected in memory."),
        new("Use o menu de contexto de uma Transaction ou execute primeiro o comando Abrir Wizard (B030).", "Use el menú contextual de una Transaction o ejecute primero el comando Abrir Wizard (B030).", "Use a Transaction context menu or run the Open Wizard command (B030) first."),
        new("Use o menu de contexto de uma Transaction ou execute primeiro o comando B022.", "Use el menú contextual de una Transaction o ejecute primero el comando B022.", "Use a Transaction context menu or run command B022 first."),
        new("Execute primeiro o comando Abrir Wizard (B030).", "Ejecute primero el comando Abrir Wizard (B030).", "Run the Open Wizard command (B030) first."),
        new("Execute primeiro o comando B022.", "Ejecute primero el comando B022.", "Run command B022 first."),
        new("A Transaction do menu de contexto não foi reencontrada na Knowledge Base ativa.", "La Transaction del menú contextual no fue reencontrada en la Knowledge Base activa.", "The context-menu Transaction was not found again in the active Knowledge Base."),
        new("A Transaction do menu de contexto nao foi reencontrada na Knowledge Base ativa.", "La Transaction del menú contextual no fue reencontrada en la Knowledge Base activa.", "The context-menu Transaction was not found again in the active Knowledge Base."),
        new("A Transaction selecionada não foi reencontrada na Knowledge Base ativa.", "La Transaction seleccionada no fue reencontrada en la Knowledge Base activa.", "The selected Transaction was not found again in the active Knowledge Base."),
        new("não foi reencontrada:", "no fue reencontrada:", "was not found again:"),
        new("Nao foi reencontrada:", "No fue reencontrada:", "Was not found again:"),
        new("Nenhuma escolha foi persistida.", "No se guardó ninguna selección.", "No selection was persisted."),
        new("Nenhum ApiPlan em memória foi encontrado.", "No se encontró ningún ApiPlan en memoria.", "No ApiPlan was found in memory."),
        new("Nenhum ApiPlan em memoria foi encontrado.", "No se encontró ningún ApiPlan en memoria.", "No ApiPlan was found in memory."),
        new("Execute e conclua primeiro o comando Abrir Wizard (B030).", "Ejecute y complete primero el comando Abrir Wizard (B030).", "Run and complete the Open Wizard command (B030) first."),
        new("Nenhuma Transaction selecionada em memoria foi encontrada.", "No se encontró ninguna Transaction seleccionada en memoria.", "No selected Transaction was found in memory."),
        new("ApiPlan em memória pertence a", "El ApiPlan en memoria pertenece a", "The ApiPlan in memory belongs to"),
        new("ApiPlan em memoria pertence a", "El ApiPlan en memoria pertenece a", "The ApiPlan in memory belongs to"),
        new("mas a seleção atual é", "pero la selección actual es", "but the current selection is"),
        new("mas a selecao atual e", "pero la selección actual es", "but the current selection is"),
        new("Execute novamente o wizard.", "Ejecute nuevamente el wizard.", "Run the wizard again."),
        new("Criacao de SDTs cancelada pelo usuario para", "Creación de SDTs cancelada por el usuario para", "SDT creation canceled by the user for"),
        new("Escrita de SDTs concluida:", "Escritura de SDTs completada:", "SDT writing completed:"),
        new("Nenhuma Procedure, API Object ou metadata persistente definitiva foi criada.", "No se creó ninguna Procedure, API Object ni metadata persistente definitiva.", "No Procedure, API Object, or definitive persistent metadata was created."),
        new("Criacao de SDTs bloqueada por preflight ou falhou antes de concluir:", "La creación de SDTs fue bloqueada por el preflight o falló antes de finalizar:", "SDT creation was blocked by preflight or failed before completion:"),
        new("Criacao de Procedures cancelada pelo usuario", "Creación de Procedures cancelada por el usuario", "Procedure creation canceled by the user"),
        new("Escrita de Procedures concluida:", "Escritura de Procedures completada:", "Procedure writing completed:"),
        new("Nenhum API Object, REST completo ou metadata persistente definitiva foi criado.", "No se creó ningún API Object, REST completo ni metadata persistente definitiva.", "No API Object, complete REST, or definitive persistent metadata was created."),
        new("Criacao de Procedures bloqueada por preflight ou falhou antes de concluir:", "La creación de Procedures fue bloqueada por el preflight o falló antes de finalizar:", "Procedure creation was blocked by preflight or failed before completion:"),
        new("Criacao de API Object cancelada pelo usuario", "Creación de API Object cancelada por el usuario", "API Object creation canceled by the user"),
        new("Escrita de API Object concluida:", "Escritura de API Object completada:", "API Object writing completed:"),
        new("Nenhum REST completo, seguranca definitiva ou metadata persistente definitiva foi criado.", "No se creó REST completo, seguridad definitiva ni metadata persistente definitiva.", "No complete REST, definitive security, or definitive persistent metadata was created."),
        new("Criacao de API Object bloqueada por preflight ou falhou antes de concluir:", "La creación de API Object fue bloqueada por el preflight o falló antes de finalizar:", "API Object creation was blocked by preflight or failed before completion:"),
        new("Descricoes aplicadas no API Object real:", "Descripciones aplicadas en el API Object real:", "Descriptions applied to the real API Object:"),
        new("Descricoes reaplicadas no API Object real durante B071-B073/B079:", "Descripciones reaplicadas en el API Object real durante B071-B073/B079:", "Descriptions reapplied to the real API Object during B071-B073/B079:"),
        new("Descricoes reaplicadas no API Object real", "Descripciones reaplicadas en el API Object real", "Descriptions reapplied to the real API Object"),
        new("Sem antecipar REST completo, codigo HTTP, seguranca definitiva ou metadata persistente.", "Sin anticipar REST completo, código HTTP, seguridad definitiva ni metadata persistente.", "Without anticipating complete REST, HTTP code, definitive security, or persistent metadata."),
        new("Metadata persistente inicial gravada:", "Metadata persistente inicial guardada:", "Initial persistent metadata saved:"),
        new("A metadata registra o snapshot do ApiPlan e dos artefatos ja aplicados; seguranca definitiva permanece fora desta etapa.", "La metadata registra la instantánea del ApiPlan y de los artefactos ya aplicados; la seguridad definitiva permanece fuera de esta etapa.", "The metadata records the ApiPlan and already-applied artifacts snapshot; definitive security remains outside this stage."),
        new("Alteracoes deliberadas pelo Wizard/Sincronizar atualizam esse baseline; alteracoes diretas nos objetos continuam bloqueadas antes de qualquer Save().", "Los cambios deliberados del Wizard/Sincronizar actualizan este baseline; los cambios directos en los objetos siguen bloqueados antes de cualquier Save().", "Intentional Wizard/Synchronize changes update this baseline; direct object changes remain blocked before any Save()."),
        new("Reexecucoes com descricoes, ownership, Service Source ou baseline divergente serao bloqueadas antes de qualquer Save().", "Las reejecuciones con descripciones, ownership, Service Source o baseline divergente se bloquearán antes de cualquier Save().", "Re-executions with divergent descriptions, ownership, Service Source, or baseline will be blocked before any Save()."),
        new("REST via Business Component aplicado e API Object sincronizado:", "REST mediante Business Component aplicado y API Object sincronizado:", "REST via Business Component applied and API Object synchronized:"),
        new("Aplicacao REST via Business Component bloqueada por preflight ou falhou antes de concluir:", "La aplicación REST mediante Business Component fue bloqueada por el preflight o falló antes de finalizar:", "REST via Business Component application was blocked by preflight or failed before completion:"),
        new("REST via Business Component nao foi aplicado para", "REST mediante Business Component no se aplicó para", "REST via Business Component was not applied for"),
        new("List aplicado e API Object sincronizado:", "List aplicado y API Object sincronizado:", "List applied and API Object synchronized:"),
        new("Aplicacao do List bloqueada por preflight ou falhou antes de concluir:", "La aplicación de List fue bloqueada por el preflight o falló antes de finalizar:", "List application was blocked by preflight or failed before completion:"),
        new("B076 requer validação HTTP em etapa separada.", "B076 requiere validación HTTP en una etapa separada.", "B076 requires HTTP validation in a separate step."),
        new("Conflitos de SDT:", "Conflictos de SDT:", "SDT conflicts:"),
        new("Diff para", "Diferencia para", "Diff for"),
        new("Nenhuma diferenca entre Transaction e metadata.", "No hay diferencias entre Transaction y metadata.", "There is no difference between Transaction and metadata."),
        new("Sincronizacao cancelada pelo usuario para", "Sincronización cancelada por el usuario para", "Synchronization canceled by the user for"),
        new("Sincronizacao bloqueada ou falhou:", "La sincronización fue bloqueada o falló:", "Synchronization was blocked or failed:"),
        new("Preflight de sincronizacao aprovado. Aplicando", "Preflight de sincronización aprobado. Aplicando", "Synchronization preflight approved. Applying"),
        new("Sincronizacao concluida para", "Sincronización completada para", "Synchronization completed for"),
        new("Plano de remocao para", "Plan de eliminación para", "Removal plan for"),
        new("Remocao cancelada pelo usuario para", "Eliminación cancelada por el usuario para", "Removal canceled by the user for"),
        new("Remocao concluida:", "Eliminación completada:", "Removal completed:"),
        new("SDTs compartilhados e Business Component da Transaction nao foram alterados.", "Los SDTs compartidos y el Business Component de la Transaction no fueron modificados.", "Shared SDTs and the Transaction Business Component were not changed."),
        new("Remocao bloqueada ou falhou:", "La eliminación fue bloqueada o falló:", "Removal was blocked or failed:"),
        new("Estado anterior do wizard descartado;", "Se descartó el estado anterior del wizard;", "Previous wizard state was discarded;"),
        new("Nenhuma etapa de escrita foi confirmada no wizard", "No se confirmó ninguna etapa de escritura en el wizard", "No writing stage was confirmed in the wizard"),
        new("Nenhuma escrita foi solicitada.", "No se solicitó ninguna escritura.", "No writing was requested."),
        new("Preflight agregado bloqueou o wizard antes do primeiro Save():", "El preflight agregado bloqueó el wizard antes del primer Save():", "Aggregated preflight blocked the wizard before the first Save():"),
        new("Preflight agregado aprovado antes do primeiro Save():", "Preflight agregado aprobado antes del primer Save():", "Aggregated preflight approved before the first Save():"),
        new("baseline da extensao ou objetos proprios ausentes, externos ou ambiguos em ", "baseline de la extensión u objetos propios ausentes, externos o ambiguos en ", "the extension baseline or owned objects missing, external, or ambiguous in "),
        new("Wizard concluído sem acionar cancelamento.", "Wizard completado sin activar la cancelación.", "Wizard completed without triggering cancellation."),
        new("Wizard concluido sem acionar cancelamento.", "Wizard completado sin activar la cancelación.", "Wizard completed without triggering cancellation."),
        new("Decisões e ApiPlan permanecem em memória.", "Las decisiones y el ApiPlan permanecen en memoria.", "Decisions and the ApiPlan remain in memory."),
        new("Decisoes e ApiPlan permanecem em memoria.", "Las decisiones y el ApiPlan permanecen en memoria.", "Decisions and the ApiPlan remain in memory."),
        new("Campos bloqueados visíveis no wizard:", "Campos bloqueados visibles en el wizard:", "Blocked fields visible in the wizard:"),
        new("Campos bloqueados visiveis no wizard:", "Campos bloqueados visibles en el wizard:", "Blocked fields visible in the wizard:"),
        new("Itens bloqueados ficaram desmarcados, com motivo, e não podem ser selecionados.", "Los elementos bloqueados quedaron desmarcados, con motivo, y no pueden seleccionarse.", "Blocked items remained unchecked with a reason and cannot be selected."),
        new("Itens bloqueados ficaram desmarcados, com motivo, e nao podem ser selecionados.", "Los elementos bloqueados quedaron desmarcados, con motivo, y no pueden seleccionarse.", "Blocked items remained unchecked with a reason and cannot be selected."),
        new("Campos sensíveis no plano:", "Campos sensibles en el plan:", "Sensitive fields in the plan:"),
        new("Campos sensiveis no plano:", "Campos sensibles en el plan:", "Sensitive fields in the plan:"),
        new("Folder '", "Folder '", "Folder '"),
        new("nao foi apagado porque nao ficou vazio.", "no se eliminó porque no quedó vacío.", "was not deleted because it was not empty."),
        new("não foi apagado porque não ficou vazio.", "no se eliminó porque no quedó vacío.", "was not deleted because it was not empty."),
        new("Business Component habilitado durante o Wizard", "Business Component habilitado durante el Wizard", "Business Component enabled during the Wizard"),
        new("Business Component habilitado durante o wizard", "Business Component habilitado durante el wizard", "Business Component enabled during the wizard"),
        new("Obrigatoriedade em memoria:", "Obligatoriedad en memoria:", "Required fields in memory:"),
        new("Obrigatoriedade em memória:", "Obligatoriedad en memoria:", "Required fields in memory:"),
        new("Required marca membro obrigatorio no payload, recusado com 400 quando ausente ou com o valor default do tipo (vazio, false ou 0).", "Required marca el miembro como obligatorio en el payload; se rechaza con 400 cuando falta o tiene el valor predeterminado del tipo (vacío, false o 0).", "Required marks a member as mandatory in the payload; it is rejected with 400 when missing or set to the type default value (empty, false, or 0)."),
        new("Required marca membro obrigatório no payload, recusado com 400 quando ausente ou com o valor default do tipo (vazio, false ou 0).", "Required marca el miembro como obligatorio en el payload; se rechaza con 400 cuando falta o tiene el valor predeterminado del tipo (vacío, false o 0).", "Required marks a member as mandatory in the payload; it is rejected with 400 when missing or set to the type default value (empty, false, or 0)."),
        new("Obrigatorio no payload consolidado:", "Obligatorio en el payload consolidado:", "Required in the consolidated payload:"),
        new("Obrigatório no payload consolidado:", "Obligatorio en el payload consolidado:", "Required in the consolidated payload:"),
        new("Create/Update respondem 400 quando o obrigatorio chega ausente ou com o valor default do tipo; o GeneXus nao expoe presenca de membro JSON sem comando csharp. UpdateRequest segue PUT completo.", "Create/Update responden 400 cuando el obligatorio llega ausente o con el valor predeterminado del tipo; GeneXus no expone la presencia de un miembro JSON sin un comando de C#. UpdateRequest sigue PUT completo.", "Create/Update return 400 when the required member is missing or has the type default value; GeneXus does not expose JSON member presence without a C# command. UpdateRequest follows a full PUT."),
        new("Create/Update respondem 400 quando o obrigatório chega ausente ou com o valor default do tipo; o GeneXus não expõe presença de membro JSON sem comando csharp. UpdateRequest segue PUT completo.", "Create/Update responden 400 cuando el obligatorio llega ausente o con el valor predeterminado del tipo; GeneXus no expone la presencia de un miembro JSON sin un comando de C#. UpdateRequest sigue PUT completo.", "Create/Update return 400 when the required member is missing or has the type default value; GeneXus does not expose JSON member presence without a C# command. UpdateRequest follows a full PUT."),
        new("Wizard único concluido em memoria:", "Asistente único completado en memoria:", "Single wizard completed in memory:"),
        new("Wizard único concluído em memória:", "Asistente único completado en memoria:", "Single wizard completed in memory:"),
        new("Contrato de API da Transacao=", "Contrato de API de la Transaction=", "Transaction API contract="),
        new("Contrato de API da Transação=", "Contrato de API de la Transaction=", "Transaction API contract="),
        new("ApiPlan cobre:", "El ApiPlan cubre:", "ApiPlan covers:"),
        new("Paths, segurança e paginacao em memoria:", "Paths, seguridad y paginación en memoria:", "Paths, security, and pagination in memory:"),
        new("Paths, segurança e paginação em memória:", "Paths, seguridad y paginación en memoria:", "Paths, security, and pagination in memory:"),
        new("Paginacao e ordenacao:", "Paginación y ordenación:", "Pagination and ordering:"),
        new("Paginacao e ordenação:", "Paginación y ordenación:", "Pagination and ordering:"),
        new("Classificacao em memoria:", "Clasificación en memoria:", "Classification in memory:"),
        new("Classificação em memória:", "Clasificación en memoria:", "Classification in memory:"),
        new("Metadata futura no ApiPlan:", "Metadata futura en el ApiPlan:", "Future metadata in the ApiPlan:"),
        new("Seguranca no ApiPlan:", "Seguridad en el ApiPlan:", "Security in the ApiPlan:"),
        new("Segurança no ApiPlan:", "Seguridad en el ApiPlan:", "Security in the ApiPlan:"),
        new("Preview de engine SDT:", "Vista previa del motor SDT:", "SDT engine preview:"),
        new("Campos de engine no ApiPlan:", "Campos del motor en el ApiPlan:", "Engine fields in the ApiPlan:"),
        new("Descricoes no ApiPlan:", "Descripciones en el ApiPlan:", "Descriptions in the ApiPlan:"),
        new("Descrições no ApiPlan:", "Descripciones en el ApiPlan:", "Descriptions in the ApiPlan:"),
        new("Sem persistir metadata e sem gerar SDT, Procedure, API Object ou File na KB.", "Sin persistir metadata ni generar SDT, Procedure, API Object o File en la KB.", "Without persisting metadata or generating an SDT, Procedure, API Object, or File in the KB."),
        new("Sem aplicar [Description] em objeto API real e sem gerar objetos.", "Sin aplicar [Description] en el API Object real ni generar objetos.", "Without applying [Description] to the real API Object or generating objects."),
        new("Sem aplicar seguranca em objetos reais.", "Sin aplicar seguridad en objetos reales.", "Without applying security to real objects."),
        new("Sem aplicar segurança em objetos reais.", "Sin aplicar seguridad en objetos reales.", "Without applying security to real objects."),
        new("Sem validar engine real e sem gerar objetos.", "Sin validar el motor real ni generar objetos.", "Without validating the real engine or generating objects."),
        new("Sem criar, alterar ou excluir objetos na KB.", "Sin crear, alterar ni eliminar objetos en la KB.", "Without creating, changing, or deleting objects in the KB."),
        new("Proximo passo habilitado para", "Siguiente paso habilitado para", "Next step enabled for"),
        new("Próximo passo habilitado para", "Siguiente paso habilitado para", "Next step enabled for"),
        new("Nenhum Save foi solicitado.", "No se solicitó ningún Save.", "No Save was requested."),
        new("A dependencia sera reencontrada e validada pelo preflight da etapa seguinte.", "La dependencia se reencontrará y validará mediante el preflight de la siguiente etapa.", "The dependency will be found again and validated by the next stage preflight."),
        new("A dependência será reencontrada e validada pelo preflight da etapa seguinte.", "La dependencia se reencontrará y validará mediante el preflight de la siguiente etapa.", "The dependency will be found again and validated by the next stage preflight."),
        new("Etapa de", "Etapa de", "Stage of"),
        new("nao foi aplicado para", "no se aplicó para", "was not applied for"),
        new("não foi aplicado para", "no se aplicó para", "was not applied for"),
        new("nao foi gravada para", "no se guardó para", "was not saved for"),
        new("não foi gravada para", "no se guardó para", "was not saved for"),
        new("porque", "porque", "because"),
        new("falhou ou foi bloqueado neste fluxo.", "falló o fue bloqueado en este flujo.", "failed or was blocked in this flow."),
        new("falhou ou foi bloqueada neste fluxo.", "falló o fue bloqueada en este flujo.", "failed or was blocked in this flow."),
        new("Como B071-B073/B079 tambem foi confirmado, a atualizacao do API Object sera absorvida pelo preflight de Business Component.", "Como B071-B073/B079 también fue confirmado, la actualización del API Object será absorbida por el preflight de Business Component.", "As B071-B073/B079 was also confirmed, the API Object update will be handled by the Business Component preflight."),
        new("tambem foi confirmado", "también fue confirmado", "was also confirmed"),
        new("tambem fue confirmado", "también fue confirmado", "was also confirmed"),
        new("foi confirmado", "fue confirmado", "was confirmed"),
        new("foi bloqueado", "fue bloqueado", "was blocked"),
        new("foi bloqueada", "fue bloqueada", "was blocked"),
        new("ja existe para", "ya existe para", "already exists for"),
        new("já existe para", "ya existe para", "already exists for"),
        new("A atualizacao do API Object sera absorvida pelo preflight de Business Component.", "La actualización del API Object será absorbida por el preflight de Business Component.", "The API Object update will be handled by the Business Component preflight."),
        new("A atualização do API Object será absorvida pelo preflight de Business Component.", "La actualización del API Object será absorbida por el preflight de Business Component.", "The API Object update will be handled by the Business Component preflight."),
        new("Nível de segurança", "Nivel de seguridad", "Security level"),
        new("Adicionados:", "Agregados:", "Added:"),
        new("Renomeados:", "Renombrados:", "Renamed:"),
        new("Modificados:", "Modificados:", "Modified:"),
        new("Inalterados:", "Sin cambios:", "Unchanged:"),
        new("tipo ", "tipo ", "type "),
        new("natureza estrutural (formula/inferido/redundante) alterada", "naturaleza estructural (fórmula/inferido/redundante) modificada", "structural nature (formula/inferred/redundant) changed"),
        new("natureza estrutural (fórmula/inferido/redundante) alterada", "naturaleza estructural (fórmula/inferido/redundante) modificada", "structural nature (formula/inferred/redundant) changed"),
        new("API Object:", "API Object:", "API Object:"),
        // O espanhol cadastrado aqui era `Archivo de metadata:`, mas a saída real sempre foi
        // `Archivo de metadatos:` — a entrada ` de metadata` alcança o próprio resultado desta,
        // porque a substituição é sequencial e reescreve texto já traduzido. O valor produzido é
        // o certo, e é o que o resto do catálogo usa; a entrada passou a declarar o que faz.
        new("Metadata File:", "Archivo de metadatos:", "Metadata File:"),
        new("Procedures (", "Procedures (", "Procedures ("),
        new("SDTs próprios (", "SDTs propios (", "Own SDTs ("),
        new("SDTs próprios", "SDTs propios", "Own SDTs"),
        new("SDTs compartilhados preservados (", "SDTs compartidos preservados (", "Preserved shared SDTs ("),
        new("SDTs compartilhados preservados", "SDTs compartidos preservados", "Preserved shared SDTs"),
        new("criado pela extensão; apagar só se ficar vazio", "creado por la extensión; eliminar solo si queda vacío", "created by the extension; delete only if it remains empty"),
        new("reutilizado; nunca apagar", "reutilizado; nunca eliminar", "reused; never delete"),
        new("Business Component da Transaction: não será revertido.", "Business Component de la Transaction: no será revertido.", "Transaction Business Component: it will not be reverted."),
        new("Business Component da Transaction: nao sera revertido.", "Business Component de la Transaction: no será revertido.", "Transaction Business Component: it will not be reverted."),
        new("Objeto principal", "Objeto principal", "Main object"),
        new("nao foi encontrado na KB.", "no se encontró en la KB.", "was not found in the KB."),
        new("não foi encontrado na KB.", "no se encontró en la KB.", "was not found in the KB."),
        new("Nao foi possivel abrir o objeto principal: ", "No fue posible abrir el objeto principal: ", "Could not open the main object: "),
        new("Não foi possível abrir o objeto principal: ", "No fue posible abrir el objeto principal: ", "Could not open the main object: "),
        new("Descricoes de servico usaram fallback em ingles", "Las descripciones de servicio usaron fallback en inglés", "Service descriptions used an English fallback"),
        new("Idioma principal da KB ainda nao validado por API publica", "El idioma principal de la KB aún no fue validado por una API pública", "The KB primary language has not yet been validated through a public API"),
        new("Atenção:", "Atención:", "Warning:"),
        new("Aviso:", "Advertencia:", "Warning:"),
        new("Criado:", "Creado:", "Created:"),
        new("Atualizado:", "Actualizado:", "Updated:"),
        new("Removido:", "Eliminado:", "Removed:"),
        new("Bloqueado:", "Bloqueado:", "Blocked:"),
        new("Causa principal: ", "Causa principal: ", "Primary cause: "),
        new("ApiObjectGuid='", "GUIDApiObject='", "APIObjectGuid='"),
        new("MetadataApiGuid='", "GUIDMetadata='", "MetadataApiGuid='"),
        new("API Object GUID atual: ", "GUID actual del API Object: ", "Current API Object GUID: "),
        new("GUID da metadata: ", "GUID de los metadatos: ", "Metadata GUID: "),
        new("Arquivo de metadata: ", "Archivo de metadatos: ", "Metadata file: "),
        new("Metadata lida como JSON: ", "Metadatos leídos como JSON: ", "Metadata read as JSON: "),
        new("Ownership da metadata: ", "Propiedad de los metadatos: ", "Metadata ownership: "),
        new("Integridade B067: ", "Integridad B067: ", "B067 integrity: "),
        new("Service Source, variáveis e Events: ", "Service Source, variables y Events: ", "Service Source, variables, and Events: "),
        new("Description fallback: ", "Fallback de Description: ", "Description fallback: "),
        new("não encontrada", "no encontrada", "not found"),
        new("encontrada e reconhecida", "encontrada y reconocida", "found and recognized"),
        new("encontrada, mas Description não reconhecida", "encontrada, pero Description no reconocida", "found, but Description not recognized"),
        new("não aplicável", "no aplicable", "not applicable"),
        new("compatível", "compatible", "compatible"),
        new("incompatível", "incompatible", "incompatible"),
        new("gerenciados", "gestionados", "managed"),
        new("ausentes=", "ausentes=", "missing="),
        new("planejados", "planificados", "planned"),
        new("A confirmacao continua obrigatoria antes de qualquer escrita.", "La confirmación sigue siendo obligatoria antes de cualquier escritura.", "Confirmation remains mandatory before any writing."),
        new("Reencontrar e validar", "Reencontrar y validar", "Re-encounter and validate"),
        new("Preferencias do wizard carregadas da KB ativa:", "Preferencias del wizard cargadas de la KB activa:", "Wizard preferences loaded from the active KB:"),
        new("Preferências do wizard carregadas da KB ativa:", "Preferencias del wizard cargadas de la KB activa:", "Wizard preferences loaded from the active KB:"),
        new("ApiPlan em memoria criado:", "ApiPlan en memoria creado:", "ApiPlan created in memory:"),
        new("ApiPlan em memória criado:", "ApiPlan en memoria creado:", "ApiPlan created in memory:"),
        new("SDT planejado:", "SDT planificado:", "Planned SDT:"),
        new("como gerador prioritario inicial do MVP", "como generador prioritario inicial del MVP", "as the MVP's initial priority generator"),
        new("para colisao externa/incompativel", "para colisión externa/incompatible", "for external/incompatible collision"),
        new("Contrato por KB preparado no ApiPlan, ainda sem metadata persistente e sem geracao.", "Contrato por KB preparado en el ApiPlan, aún sin metadatos persistentes y sin generación.", "KB contract prepared in the ApiPlan, still without persistent metadata and without generation."),
        new("Ainda sem ler ou gravar File de metadata.", "Aún sin leer ni grabar el File de metadatos.", "Still without reading or writing the metadata File."),
        new("Business Component em memoria:", "Business Component en memoria:", "Business Component in memory:"),
        new("Business Component em memória:", "Business Component en memoria:", "Business Component in memory:"),
        new("escritas confirmadas no wizard exigem preflight completo antes de qualquer Save().", "las escrituras confirmadas en el wizard exigen preflight completo antes de cualquier Save().", "writes confirmed in the wizard require a complete preflight before any Save()."),
        new("Metadata de integridade gravada:", "Metadata de integridad guardada:", "Integrity metadata saved:"),
        new("Status HTTP controlado por RestCode no API Object; ErrorResponse exposto como saida publica dos servicos; Location de Create emitido nativamente via HttpResponse.", "Status HTTP controlado por RestCode en el API Object; ErrorResponse expuesto como salida pública de los servicios; Location de Create emitido nativamente vía HttpResponse.", "HTTP status controlled by RestCode on the API Object; ErrorResponse exposed as a public service output; Create Location emitted natively via HttpResponse."),
        new("Service Source preserva o contrato Procedure/API Object atual.", "Service Source conserva el contrato Procedure/API Object actual.", "Service Source preserves the current Procedure/API Object contract."),
        new("idioma da KB ainda nao validado por API publica", "idioma de la KB aún no validado por API pública", "KB language not yet validated by a public API"),
        new("fallback tecnico em ingles registrado no ApiPlan", "fallback técnico en inglés registrado en el ApiPlan", "technical English fallback recorded in the ApiPlan"),
        new("Folder preexistente '", "Carpeta preexistente '", "Pre-existing folder '"),
        new("no contenedor correto sera reutilizado; a Description existente sera preservada e o Folder nunca sera removido pela remocao desta API.", "en el contenedor correcto será reutilizado; la Description existente se preservará y el Folder nunca será eliminado por la eliminación de esta API.", "in the correct container will be reused; the existing Description will be preserved and the Folder will never be removed by removing this API."),
        new("Apta via Business Component", "Apta mediante Business Component", "Ready via Business Component"),
        new("divergentes", "divergentes", "divergent"),
        new("Posse confirmada pela metadata.", "Propiedad confirmada por los metadatos.", "Ownership confirmed by metadata."),
        new("Posse confirmada pela Description fallback.", "Propiedad confirmada por el fallback de Description.", "Ownership confirmed by Description fallback."),
        new("Há mais de um arquivo de metadata com o mesmo nome.", "Hay más de un archivo de metadatos con el mismo nombre.", "More than one metadata file has the same name."),
        new("A Description do arquivo de metadata não é reconhecida como própria.", "La Description del archivo de metadatos no se reconoce como propia.", "The metadata file Description is not recognized as owned."),
        new("O arquivo de metadata próprio não pôde ser lido como JSON.", "No se pudo leer como JSON el archivo de metadatos propio.", "The owned metadata file could not be read as JSON."),
        new("O GUID atual do API Object diverge do GUID registrado na metadata.", "El GUID actual del API Object difiere del GUID registrado en los metadatos.", "The current API Object GUID differs from the GUID recorded in metadata."),
        new("O ownership registrado na metadata não corresponde ao API Object atual.", "La propiedad registrada en los metadatos no corresponde al API Object actual.", "The ownership recorded in metadata does not match the current API Object."),
        new("A integridade B067 da metadata não corresponde ao estado atual.", "La integridad B067 de los metadatos no corresponde al estado actual.", "The metadata B067 integrity does not match the current state."),
        new("Service Source, variáveis ou Events não correspondem ao contrato gerenciado.", "Service Source, variables o Events no corresponden al contrato gestionado.", "Service Source, variables, or Events do not match the managed contract."),
        new("A Description do API Object não corresponde a nenhum fallback próprio.", "La Description del API Object no corresponde a ningún fallback propio.", "The API Object Description does not match any owned fallback."),
        new("Nenhuma sincronizacao necessaria.", "Ninguna sincronización necesaria.", "No synchronization is necessary."),
        new("Nenhuma sincronização necessária.", "Ninguna sincronización necesaria.", "No synchronization is necessary."),
        new("a atualizacao do API Object sera absorvida pelo preflight de Business Component.", "la actualización del API Object será absorbida por el preflight de Business Component.", "the API Object update will be handled by the Business Component preflight."),
        new("a atualização do API Object será absorvida pelo preflight de Business Component.", "la actualización del API Object será absorbida por el preflight de Business Component.", "the API Object update will be handled by the Business Component preflight."),
        new(" em memoria:", " en memoria:", " in memory:"),
        new(" em memória:", " en memoria:", " in memory:"),
        new("colisao(oes) externa(s), incompativel(is), ambigua(s) ou integridade B067 divergente", "colisión(es) externa(s), incompatible(s), ambigua(s) o integridad B067 divergente", "external, incompatible, ambiguous collision(s), or divergent B067 integrity"),
        new("colisao(oes) externa(s), incompativel(is) ou ambigua(s)", "colisión(es) externa(s), incompatible(s) o ambigua(s)", "external, incompatible, or ambiguous collision(s)"),
        new("detectada(s).", "detectada(s).", "detected."),
        new("Nenhuma escrita sera permitida.", "No se permitirá ninguna escritura.", "No writing will be allowed."),
        new("Nenhuma escrita será permitida.", "No se permitirá ninguna escritura.", "No writing will be allowed."),
        new("Etapa '", "Etapa '", "Stage '"),
        new(" bloqueada na KB:", " bloqueada en la KB:", " blocked in the KB:"),
        new("Conflitos (", "Conflictos (", "Conflicts ("),
        new("Nome='", "Nombre='", "Name='"),
        new("Tipo='", "Tipo='", "Type='"),
        new("Modulo='", "Módulo='", "Module='"),
        new("o API Object precisa estar disponivel antes.", "el API Object debe estar disponible antes.", "the API Object must be available first."),
        new("o estado dos SDTs precisa ser resolvido antes.", "el estado de los SDTs debe resolverse antes.", "the SDT state must be resolved first."),
        new("o estado dos SDTs ou Procedures precisa ser resolvido antes.", "el estado de los SDTs o Procedures debe resolverse antes.", "the SDT or Procedure state must be resolved first."),
        new("Diagnostico de posse do API Object (baseline de alteracao intencional):", "Diagnóstico de propiedad del API Object (baseline de cambio intencional):", "API Object ownership diagnostic (intentional-change baseline):"),
        new("Diagnostico de posse do API Object:", "Diagnóstico de propiedad del API Object:", "API Object ownership diagnostic:"),
        new("etapa bloqueada sem lista de conflito.", "etapa bloqueada sin lista de conflicto.", "stage blocked without a conflict list."),
        new("ClausulaQueFalhou=", "ClausulaQueFallo=", "FailingClause="),
        new("MetadataPresente=", "MetadataPresente=", "MetadataPresent="),
        new("MetadataDescriptionPropria=", "MetadataDescriptionPropia=", "MetadataDescriptionOwned="),
        new("IntegrityPresente=", "IntegrityPresente=", "IntegrityPresent="),
        new("FingerprintPresente=", "FingerprintPresente=", "FingerprintPresent="),
        new("FingerprintAlgoritmoOk=", "FingerprintAlgoritmoOk=", "FingerprintAlgorithmOk="),
        new("FingerprintEscopoOk=", "FingerprintEscopoOk=", "FingerprintScopeOk="),
        new("FingerprintValorPresente=", "FingerprintValorPresente=", "FingerprintValuePresent="),
        new("FingerprintHashOk=", "FingerprintHashOk=", "FingerprintHashOk="),
        new("FingerprintDetalhe=", "FingerprintDetalle=", "FingerprintDetail="),
        new("FingerprintAlgoritmo=", "FingerprintAlgoritmo=", "FingerprintAlgorithm="),
        new("FingerprintEscopo=", "FingerprintEscopo=", "FingerprintScope="),
        new("FingerprintGravado=", "FingerprintGrabado=", "FingerprintStored="),
        new("FingerprintRecalculado=", "FingerprintRecalculado=", "FingerprintActual="),
        new("SchemaGravado=", "SchemaGrabado=", "StoredSchema="),
        new("ApiNameGravado=", "ApiNameGrabado=", "StoredApiName="),
        new("ApiNameEsperado=", "ApiNameEsperado=", "ExpectedApiName="),
        new("DescriptionAtual=", "DescriptionActual=", "CurrentDescription="),
        new("DescriptionSentinel=", "DescriptionSentinel=", "DescriptionSentinel="),
        new("ServiceSourceHashAtual=", "ServiceSourceHashActual=", "CurrentServiceSourceHash="),
        new("ServiceSourceHashGravado=", "ServiceSourceHashGrabado=", "StoredServiceSourceHash="),
        new("DescriptionsHashAtual=", "DescriptionsHashActual=", "CurrentDescriptionsHash="),
        new("DescriptionsHashGravado=", "DescriptionsHashGrabado=", "StoredDescriptionsHash="),
        new("Nenhum ApiPlan foi criado, nenhuma escolha foi persistida e nenhum objeto foi criado, alterado ou excluido.", "No se creó ningún ApiPlan, no se persistió ninguna elección y no se creó, alteró ni eliminó ningún objeto.", "No ApiPlan was created, no selection was persisted, and no object was created, changed, or deleted."),
        new("nenhum ApiPlan foi criado e nenhuma alteracao foi feita na KB.", "no se creó ningún ApiPlan y no se realizaron cambios en la KB.", "no ApiPlan was created and no changes were made to the KB."),
        new("Nenhum ApiPlan foi criado e nenhuma alteracao foi feita na KB.", "No se creó ningún ApiPlan y no se realizaron cambios en la KB.", "No ApiPlan was created and no changes were made to the KB."),
        new("nenhum ApiPlan foi criado.", "no se creó ningún ApiPlan.", "no ApiPlan was created."),
        new("Nenhum ApiPlan foi criado.", "No se creó ningún ApiPlan.", "No ApiPlan was created."),
        new("Voltar acionado no início do wizard único.", "Se activó Atrás al inicio del asistente único.", "Back was used at the start of the single wizard."),
        new("Voltar acionado durante o fluxo B032.", "Se activó Atrás durante el flujo B032.", "Back was used during the B032 flow."),
        new("Voltar acionado no Passo 3.", "Se activó Atrás en el Paso 3.", "Back was used on Step 3."),
        new("Voltar acionado no Passo 2.", "Se activó Atrás en el Paso 2.", "Back was used on Step 2."),
        new("e decisões em memoria foram descartadas;", "y las decisiones en memoria se descartaron;", "and in-memory decisions were discarded;"),
        new("e decisões em memória foram descartadas;", "y las decisiones en memoria se descartaron;", "and in-memory decisions were discarded;"),
        new("Transaction e decisões em memoria descartadas;", "Transaction y decisiones en memoria descartadas;", "Transaction and in-memory decisions discarded;"),
        new("Transaction e decisões em memória descartadas;", "Transaction y decisiones en memoria descartadas;", "Transaction and in-memory decisions discarded;"),
        new("Wizard único cancelado ou fechado para", "Asistente único cancelado o cerrado para", "Single wizard canceled or closed for"),
        new("Wizard único fechado sem conclusao para", "Asistente único cerrado sin concluir para", "Single wizard closed without completion for"),
        new("Wizard único fechado sem conclusão para", "Asistente único cerrado sin concluir para", "Single wizard closed without completion for"),
        new("Estado em memoria descartado;", "Estado en memoria descartado;", "In-memory state discarded;"),
        new("Estado em memória descartado;", "Estado en memoria descartado;", "In-memory state discarded;"),
        new("Estado do wizard descartado;", "Estado del wizard descartado;", "Wizard state discarded;"),
        new("Business Component foi habilitado por confirmacao explicita antes da saida; essa alteracao foi gravada na KB e nao foi revertida automaticamente.", "El Business Component fue habilitado por confirmación explícita antes de salir; ese cambio se grabó en la KB y no se revirtió automáticamente.", "Business Component was enabled by explicit confirmation before exit; that change was saved to the KB and was not reverted automatically."),
        new("permaneceu selecionada em memoria;", "permaneció seleccionada en memoria;", "remained selected in memory;"),
        new("permaneceu selecionada em memória;", "permaneció seleccionada en memoria;", "remained selected in memory;"),
        new("nenhuma escolha de contrato foi persistida.", "no se persistió ninguna elección de contrato.", "no contract selection was persisted."),
        new("Wizard cancelado durante o fluxo B032 para", "Wizard cancelado durante el flujo B032 para", "Wizard canceled during the B032 flow for"),
        new("Wizard cancelado no Passo 3 para", "Wizard cancelado en el Paso 3 para", "Wizard canceled on Step 3 for"),
        new("Wizard cancelado no Passo 2 para", "Wizard cancelado en el Paso 2 para", "Wizard canceled on Step 2 for"),
        new("Escolhas em memoria descartadas;", "Selecciones en memoria descartadas;", "In-memory selections discarded;"),
        new("Escolhas em memória descartadas;", "Selecciones en memoria descartadas;", "In-memory selections discarded;"),
        new("Passo 2 fechado sem conclusao durante o fluxo B032 para", "Paso 2 cerrado sin concluir durante el flujo B032 para", "Step 2 closed without completion during the B032 flow for"),
        new("Passo 2 fechado sem conclusao para", "Paso 2 cerrado sin concluir para", "Step 2 closed without completion for"),
        new("Passo 2 fechado sem conclusão para", "Paso 2 cerrado sin concluir para", "Step 2 closed without completion for"),
        new("Passo 3 fechado sem conclusao para", "Paso 3 cerrado sin concluir para", "Step 3 closed without completion for"),
        new("Passo 3 fechado sem conclusão para", "Paso 3 cerrado sin concluir para", "Step 3 closed without completion for"),
        new("Wizard Passo 2 concluido em memoria durante o fluxo B032:", "Wizard Paso 2 completado en memoria durante el flujo B032:", "Wizard Step 2 completed in memory during the B032 flow:"),
        new("Wizard Passo 2 concluido em memoria:", "Wizard Paso 2 completado en memoria:", "Wizard Step 2 completed in memory:"),
        new("Wizard Passo 3 concluido em memoria:", "Wizard Paso 3 completado en memoria:", "Wizard Step 3 completed in memory:"),
        new("Reabrindo B031 para", "Reabriendo B031 para", "Reopening B031 for"),
        new("sem persistir escolhas.", "sin persistir selecciones.", "without persisting selections."),
        new("A Transaction selecionada nao possui modulo disponivel:", "La Transaction seleccionada no tiene un módulo disponible:", "The selected Transaction has no available module:"),
        new("Transaction resolvida para o wizard:", "Transaction resuelta para el wizard:", "Transaction resolved for the wizard:"),
        new("Service Source do API Object declara servico duplicado:", "El Service Source del API Object declara un servicio duplicado:", "The API Object Service Source declares a duplicated service:"),
        new("O wizard usou a primeira declaracao de cada servico e nenhuma alteracao foi feita na KB; revise o API Object na IDE.", "El wizard usó la primera declaración de cada servicio y no se realizaron cambios en la KB; revise el API Object en el IDE.", "The wizard used the first declaration of each service and no changes were made to the KB; review the API Object in the IDE."),
        new("Servicos='", "Servicios='", "Services='"),
        new("Contrato B031 ausente ou incompativel para", "Contrato B031 ausente o incompatible para", "B031 contract missing or incompatible for"),
        new("Contrato B031 ausente para", "Contrato B031 ausente para", "B031 contract missing for"),
        new("Abrindo B031 automaticamente.", "Abriendo B031 automáticamente.", "Opening B031 automatically."),
        new("Campos selecionados:", "Campos seleccionados:", "Selected fields:"),
        new("nenhuma escolha foi persistida.", "no se persistió ninguna selección.", "no selection was persisted."),
        new("nenhuma escolha foi mantida.", "no se conservó ninguna selección.", "no selection was kept."),
        new("nenhuma alteracao foi feita na KB.", "no se realizaron cambios en la KB.", "no changes were made to the KB."),
        new("bloqueada: Business Component desabilitado e habilitacao explicita nao confirmada.", "bloqueada: Business Component deshabilitado y habilitación explícita no confirmada.", "blocked: Business Component disabled and explicit enablement not confirmed."),
        new("Habilitacao de Business Component cancelada para", "Habilitación de Business Component cancelada para", "Business Component enablement canceled for"),
        new("Falha ao confirmar Business Component habilitado para", "Fallo al confirmar Business Component habilitado para", "Failed to confirm Business Component enabled for"),
        new("apos gravacao.", "después de la grabación.", "after saving."),
        new("Falha ao habilitar Business Component para", "Fallo al habilitar Business Component para", "Failed to enable Business Component for"),
        new("Business Component habilitado por confirmacao explicita para", "Business Component habilitado por confirmación explícita para", "Business Component enabled by explicit confirmation for"),
        new("A alteracao foi gravada na KB e nao sera revertida automaticamente.", "El cambio se grabó en la KB y no se revertirá automáticamente.", "The change was saved to the KB and will not be reverted automatically."),
        new("Falha ao ler a estrutura hierárquica da Transaction", "Fallo al leer la estructura jerárquica de la Transaction", "Failed to read the hierarchical structure of Transaction"),
        new("O Wizard segue no caminho de nível único (sem subníveis).", "El Wizard continúa en el camino de nivel único (sin subniveles).", "The Wizard continues on the single-level path (no sublevels)."),
        new("Detalhe:", "Detalle:", "Detail:"),
        new("Nenhum", "Ningún", "No"),
    };

    /// <summary>
    /// A ordem em que as frases são **aplicadas**, que não é a ordem em que foram escritas.
    ///
    /// A substituição é por substring, então uma entrada curta aplicada antes de uma longa que a
    /// contenha recorta o meio da longa: quando a longa chega, o texto já mudou e ela não casa
    /// mais. O resultado é a pior falha possível para texto — não quebra, não lança, e produz
    /// frase plausível meio em cada idioma.
    ///
    /// Isso era administrado à mão, por comentário: «um fragmento curto cadastrado antes
    /// recortaria o meio delas». Disciplina na cabeça de quem edita não sobrevive a um catálogo
    /// desta escala — eram 664 entradas na data da medição abaixo, e ele só cresce.
    /// Medido em 2026-09-15, antes desta ordenação: **oito frases** saíam corrompidas, em quinze
    /// combinações frase/idioma — `Arquivo de metadata: ` virava `Arquivo metadata: ` em inglês,
    /// porque ` de metadata` estava cadastrada dezenas de linhas antes.
    ///
    /// Ordenar por comprimento decrescente elimina a classe inteira: se A é substring de B então
    /// A é mais curta que B, logo B é aplicada primeiro e casa. `OrderByDescending` do LINQ é
    /// estável, então entradas de mesmo comprimento preservam a ordem de escrita — inclusive a
    /// resolução de duplicatas, onde a primeira continua vencendo.
    ///
    /// O array literal acima continua sendo a fonte de verdade editável; esta é só a ordem de
    /// aplicação, derivada dele uma vez. O gate `tests.outputLocalizationSelfConsistency` exerce
    /// o catálogo contra si mesmo e falha se a classe voltar.
    /// </summary>
    private static readonly Phrase[] OrderedPhrases = Phrases
        .OrderByDescending(phrase => phrase.Source.Length)
        .ToArray();

    public static string Translate(string message, ExtensionLanguage language)
    {
        if (string.IsNullOrEmpty(message) || language == ExtensionLanguage.PortugueseBrazil)
        {
            return message;
        }

        var localized = message;
        foreach (var phrase in OrderedPhrases)
        {
            localized = localized.Replace(
                phrase.Source,
                language == ExtensionLanguage.Spanish ? phrase.Spanish : phrase.English);
        }

        return localized;
    }
}
