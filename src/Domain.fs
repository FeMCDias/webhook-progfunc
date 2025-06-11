namespace Webhook.Domain

open System.Text.Json.Serialization

/// Payload recebido do gateway
[<CLIMutable>]
type PaymentEvent =
    { [<JsonPropertyName("event")>]          Event         : string
      [<JsonPropertyName("transaction_id")>] TransactionId : string
      [<JsonPropertyName("amount")>]         Amount        : string
      [<JsonPropertyName("currency")>]       Currency      : string
      [<JsonPropertyName("timestamp")>]      Timestamp     : string }
