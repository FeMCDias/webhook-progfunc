# Webhook Payment Listener · F# / .NET 9

Projeto individual para a disciplina **Programação Funcional – 2025/1** (prof. Raul Ikeda).
Implementa, em estilo funcional, a integração de um gateway de pagamento via **Webhook**. Todo o ciclo – recepção, validação, confirmação/cancelamento e idempotência – foi codificado em F# mantendo funções puras onde possível e encapsulando efeitos colaterais.

> ✅ Passa no *test suite* oficial (`test_webhook.py`) — **6 / 6 cenários**.

---

## 1 · Visão Técnica

| Etapa                    | Descrição                                                                                                                         |
| ------------------------ | --------------------------------------------------------------------------------------------------------------------------------- |
| **Recepção**             | Rota `POST /webhook` (Kestrel, porta 5000) consome JSON do gateway.                                                               |
| **Autenticação**         | Header `X-Webhook-Token` deve conter o segredo. Token inválido ⇒ HTTP 401.                                                        |
| **Desserialização**      | Corpo convertido em `PaymentEvent` (campos: `event`, `transaction_id`, `amount`, `currency`, `timestamp`).                        |
| **Validação do payload** | Função pura em `Pure.fs` verifica presença de campos, tipo correto e `amount > 0`.                                                |
| **Idempotência**         | Dicionário em memória (`ConcurrentDictionary`) armazena transações já confirmadas. Apenas **transações válidas** são registradas. |
| **Confirmação**          | Se válida e inédita ⇒ HTTP 200 e `POST /confirmar` (porta 5001).                                                                  |
| **Cancelamento**         | Se inválida ⇒ HTTP 422 e `POST /cancelar`.                                                                                        |
| **Duplicatas**           | Se transação já confirmada ⇒ HTTP 409 (não chama end‑points externos).                                                            |

### Fluxo resumido

```text
┌──────────┐     POST /webhook      ┌──────────────┐
│  GATEWAY │ ─────────────────────▶ │  SERVIDOR F# │
└──────────┘                        │              │
                                    │  validação   │
                                    │  idempotência│
                                    └──────┬───────┘
                                           │
       ┌────────────────────┬──────────────┴───────────────┐
       │                    │                              │
   válido + inédito     duplicado (ID)                 inválido
       │                    │                              │
HTTP 200 + /confirmar   HTTP 409                      HTTP 422 + /cancelar
```

*Todos os efeitos (chamadas externas) estão em `Infra.fs`; lógica de domínio pura é testável isoladamente.*

---

## 2 · Estrutura de Pastas

```
project-webhook/
 ├─ src/
 │   ├─ Domain.fs   # DTO do payload
 │   ├─ Pure.fs     # validações e regras puras
 │   ├─ Storage.fs  # idempotência em memória
 │   ├─ Infra.fs    # efeitos colaterais (HTTP outbound)
 │   ├─ Program.fs  # bootstrap + rotas
 │   └─ Webhook.fsproj
 ├─ test_webhook.py # suíte oficial da disciplina
 ├─ requirements.txt
 └─ README.md
```

Arquivos F# são declarados em ordem de dependência no `.fsproj`, garantindo compile‑time safety.

---

## 3 · Pré‑requisitos

| Ferramenta         | Versão                 | Motivo                            |
| ------------------ | ---------------------- | --------------------------------- |
| **.NET 9 SDK**     | preview 1+             | Compilar / rodar o servidor F#.   |
| **Python 3.8+**    | —                      | Rodar `test_webhook.py`.          |
| **Pacotes Python** | ver `requirements.txt` | `fastapi`, `uvicorn`, `requests`. |

Instalação rápida dos pacotes Python:

```bash
python -m venv .venv
source .venv/bin/activate      # Windows: .venv\Scripts\activate
pip install -r requirements.txt
```

---

## 4 · Como Rodar

### 4.1 Servidor F\#

```bash
cd project-webhook/src
dotnet clean            # limpa artefatos anteriores
dotnet restore          # restaura pacotes NuGet
dotnet run              # escuta em http://127.0.0.1:5000/webhook
```

### 4.2 Serviço Dummy de Confirmação / Cancelamento Serviço Dummy de Confirmação / Cancelamento

O script de teste levanta automaticamente um FastAPI em `127.0.0.1:5001` com
rotas `/confirmar` e `/cancelar`. Nada a fazer manualmente.

### 4.3 Rodando os testes

```bash
python test_webhook.py            # terminal com venv ativo
```

Saída esperada:

```
1. Webhook test ok: successful!
2. Webhook test ok: transação duplicada!
3. Webhook test ok: amount incorreto!
4. Webhook test ok: Token Invalido!
5. Webhook test ok: Payload Invalido!
6. Webhook test ok: Campos ausentes!
6/6 tests completed.
```

> **Adendo — idempotência durante testes**
>
> * O serviço mantém em memória os `transaction_id` **validados**. Se você rodar `test_webhook.py` duas vezes sem reiniciar o servidor, o **primeiro teste** (fluxo "successful") falhará, pois a transação `abc123` já terá sido marcada como concluída (o servidor responderá 409).
> * Para repetir a suíte com 6/6 passes, **reinicie o servidor** (`Ctrl‑C` e `dotnet run` novamente) **ou** passe um `transaction_id` diferente:
>
>   ```bash
>   python test_webhook.py payment_success xyz789
>   ```
> * Rode o script de teste em **outro terminal** que esteja usando o **mesmo ambiente virtual** (venv) em que você instalou os pacotes.

## 5 · Extensões Futuras

| Feature               | Descrição                                                                                         |
| --------------------- | ------------------------------------------------------------------------------------------------- |
| **Persistência**      | Substituir `Storage.fs` por um repositório PostgreSQL/Mongo para idempotência pós‑reinício.       |
| **HTTPS**             | Configurar Kestrel com certificado de desenvolvimento (`UseHttps`) para criptografia em trânsito. |
| **Retry / Filas**     | Enfileirar confirmações/cancelamentos em RabbitMQ ou AWS SQS para resiliência.                    |                                    

---

## 6 · Auxílio de IA
ChatGPT: documentação, debugging mais complexos, readme.md. Tudo que foi auxiliado foi revisado por mim e testado.
