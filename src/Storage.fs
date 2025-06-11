namespace Webhook.Storage

open System.Collections.Concurrent

/// Guarda transações já processadas (idempotência simples em RAM)
module Transactions =

    let private processed = ConcurrentDictionary<string, unit>()

    /// Tenta reservar o transaction_id; devolve true se era inédito
    let tryReserve (id: string) : bool =
        processed.TryAdd(id, ())   // true → ainda não existia
