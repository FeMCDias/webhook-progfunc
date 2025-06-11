open System
open System.IO
open System.Text
open System.Text.Json
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Primitives

open Webhook.Domain
open Webhook.Pure
open Webhook.Storage
open Webhook.Infra

let builder = WebApplication.CreateBuilder()
let app     = builder.Build()

let secretToken = StringValues "meu-token-secreto"

// rota principal
app.MapPost("/webhook", Func<HttpContext, _>(fun ctx ->
    task {
        // autenticação
        match ctx.Request.Headers.TryGetValue "X-Webhook-Token" with
        | false, _ -> ctx.Response.StatusCode <- 401
        | true, t when t <> secretToken -> ctx.Response.StatusCode <- 401
        | _ ->
            // corpo da requisição
            use sr = new StreamReader(ctx.Request.Body, Encoding.UTF8)
            let! body = sr.ReadToEndAsync()

            // desserialização
            let opts = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
            let paymentOpt =
                JsonSerializer.Deserialize<PaymentEvent>(body, opts) |> Option.ofObj

            match paymentOpt with
            | None -> ctx.Response.StatusCode <- 400
            | Some p ->
                if Validation.isValid p then
                    if Transactions.tryReserve p.TransactionId then
                        do! RemoteCalls.confirm p.TransactionId
                        ctx.Response.StatusCode <- 200
                    else
                        ctx.Response.StatusCode <- 409
                else
                    do! RemoteCalls.cancel p.TransactionId
                    ctx.Response.StatusCode <- 422
        return ()
    })) |> ignore

app.Run("http://127.0.0.1:5000")