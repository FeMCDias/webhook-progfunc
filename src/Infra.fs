namespace Webhook.Infra

open System
open System.Net.Http
open System.Text
open System.Text.Json
open System.Threading.Tasks

module RemoteCalls =

    let private http = new HttpClient()

    let private post (url: string) (transactionId: string) =
        task {
            let body     = {| transaction_id = transactionId |}
            let json     = JsonSerializer.Serialize body
            use content  = new StringContent(json, Encoding.UTF8, "application/json")

            // Usa Uri explícito e descarta o resultado (_)
            let uri = Uri(url)
            let! _ = http.PostAsync(uri, content)
            return ()
        }

    let confirm = post "http://localhost:5001/confirmar"
    let cancel  = post "http://localhost:5001/cancelar"
