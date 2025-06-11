open System
open System.IO
open System.Text
open System.Text.Json
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Primitives       // StringValues

open Webhook.Domain
open Webhook.Pure
open Webhook.Storage
open Webhook.Infra

let builder = WebApplication.CreateBuilder()
let app     = builder.Build()

// 🔐 token secreto já como StringValues → evita conversão implícita (FS3391)
let secretToken = StringValues "meu-token-secreto"

app.MapPost("/webhook", Func<HttpContext, _>(fun ctx ->
    task {
        // 1. Autenticação simples
        match ctx.Request.Headers.TryGetValue "X-Webhook-Token" with
        | false, _ ->
            ctx.Response.StatusCode <- 401
        | true, token when token <> secretToken ->
            ctx.Response.StatusCode <- 401
        | _ ->
            // 2. Leitura do corpo
            use sr = new StreamReader(ctx.Request.Body, Encoding.UTF8)
            let! body = sr.ReadToEndAsync()

            // 3. Desserializa para Option → some/none elimina warnings de nulidade (FS3261)
            let opts = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
            let paymentOpt =
                JsonSerializer.Deserialize<PaymentEvent>(body, opts)
                |> Option.ofObj

            match paymentOpt with
            | None ->
                ctx.Response.StatusCode <- 400
            | Some p ->
                // 4. Idempotência
                if not (Transactions.tryReserve p.TransactionId) then
                    ctx.Response.StatusCode <- 409        // já processada
                // 5. Validação
                elif Validation.isValid p then
                    do! RemoteCalls.confirm p.TransactionId
                    ctx.Response.StatusCode <- 200
                else
                    do! RemoteCalls.cancel  p.TransactionId
                    ctx.Response.StatusCode <- 422        // payload ruim
        return ()
    })) |> ignore   // remove FS0020

// HTTPS fora do escopo
app.Run("http://127.0.0.1:5000")
