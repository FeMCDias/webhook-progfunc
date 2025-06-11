namespace Webhook.Pure

open System
open System.Globalization
open Webhook.Domain

module Validation =

    let private required (p: PaymentEvent) =
        not (String.IsNullOrWhiteSpace p.Event)
        && not (String.IsNullOrWhiteSpace p.TransactionId)
        && not (String.IsNullOrWhiteSpace p.Currency)
        && not (String.IsNullOrWhiteSpace p.Timestamp)
        && not (String.IsNullOrWhiteSpace p.Amount)

    /// conversão
    let private positiveAmount (p: PaymentEvent) =
        match Decimal.TryParse(p.Amount,
                               NumberStyles.AllowDecimalPoint,
                               CultureInfo.InvariantCulture) with
        | true, v when v > 0m -> true
        | _                   -> false

    let isValid p = required p && p.Event = "payment_success" && positiveAmount p
