# 🦙 Ollama.Net

Modern, async, AOT-friendly .NET client for the [Ollama](https://ollama.com) REST API —
supports text generation, chat, embeddings, streaming, tool calling, and full
model management. Targets **.NET 8 / 9 / 10**.

```bash
dotnet add package OllamaNet.Client
```

> The NuGet package ID is **`OllamaNet.Client`**; the assembly and root
> namespace are still `Ollama.Net`, so your `using` directives are
> unchanged.

## Features

- ✅ **Idiomatic .NET** — `IHttpClientFactory`, `IOptions<T>`, `ILogger<T>`, `CancellationToken`
- ✅ **AOT + trim friendly** — `System.Text.Json` source generators only
- ✅ **First-class streaming** via `IAsyncEnumerable<T>`
- ✅ **Tool calling** with strongly-typed records
- ✅ **Structured outputs** — `OllamaFormat` (JSON mode or JSON-schema object)
- ✅ **Thinking models** — `Think` toggle + `OllamaMessage.Thinking` round-trip
- ✅ **Embeddings** with batch support
- ✅ **Full model management** (list, show, pull, push, create, delete, copy, ps, blobs)
- ✅ **Ollama Cloud** support with correct `402`/`429` handling
- ✅ **15+ typed exceptions** for narrow `catch` blocks
- ✅ **OpenTelemetry** `ActivitySource` + `Meter` (named `Ollama.Net`)
- ✅ **Automatic retries** via `Microsoft.Extensions.Http.Resilience`
- ✅ **Secure by default** — HTTPS enforced for non-loopback; no cookies

## Quick Start

### 1. Register with dependency injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ollama.Net.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOllamaClient(options =>
{
    options.BaseAddress  = new Uri("http://localhost:11434/");
    options.DefaultModel = "llama3.2";
    options.Timeout      = TimeSpan.FromSeconds(120);
});

var host = builder.Build();
```

### 2. Generate text

```csharp
using Microsoft.Extensions.DependencyInjection;
using Ollama.Net.Abstractions;
using Ollama.Net.Models.Requests;

var client = host.Services.GetRequiredService<IOllamaClient>();

var response = await client.Generation.GenerateAsync(
    new GenerateRequest(Model: "llama3.2", Prompt: "Why is the sky blue?"));

Console.WriteLine(response.Response);
```

### 3. Stream a chat

```csharp
using Ollama.Net.Models.Common;

var request = new ChatRequest(
    Model: "llama3.2",
    Messages: [ new OllamaMessage(OllamaRole.User, "Tell me a joke about C#.") ]);

await foreach (var chunk in client.Generation.ChatStreamAsync(request))
    Console.Write(chunk.Message.Content);
```

### 4. Generate embeddings

```csharp
var result = await client.Embeddings.EmbedAsync(
    new EmbedRequest(Model: "nomic-embed-text",
                     Input: ["The quick brown fox", "jumps over the lazy dog"]));
```

### 5. Model management

```csharp
await foreach (var p in client.Models.PullModelStreamAsync(new PullModelRequest("llama3.2")))
    Console.WriteLine($"{p.Status}: {p.Completed}/{p.Total}");

var models = await client.Models.ListModelsAsync();
```

## Configuration options

| Property | Default | Description |
|----------|---------|-------------|
| `BaseAddress` | `http://localhost:11434/` | Ollama server URL |
| `DefaultModel` | `null` | Default model name (optional) |
| `Timeout` | `100s` | Request timeout (non-streaming) |
| `MaxRetries` | `2` | Max retry attempts (0-10) |
| `KeepAlive` | `5min` | How long to keep models in memory |
| `UserAgent` | `"Ollama.Net"` | User-Agent header |
| `AuthorizationHeader` | `null` | Raw `Authorization` header; takes precedence over `ApiKey` |
| `ApiKey` | `null` | Shortcut for `Authorization: Bearer {ApiKey}` — use for Ollama Cloud |
| `AllowInsecureHttp` | `false` | Allow HTTP to non-loopback |
| `DisallowPrivateNetworks` | `false` | Reject connections whose DNS-resolved IP falls in RFC1918 / link-local / CGNAT / unique-local / multicast ranges (evaluated per hop, incl. redirects). SSRF hardening beyond what `AllowInsecureHttp` covers. |

### Secret rotation

`ApiKey` and `AuthorizationHeader` are read through `IOptionsMonitor<OllamaClientOptions>`
on every outbound request, so rotating them via `IOptionsMonitor`, `PostConfigure`, or
a reloaded configuration source is picked up immediately — no need to rebuild the DI
container or recreate the `IOllamaClient`.

### HttpClient customisation (SSRF `ConnectCallback`, proxies, etc.)

If you need full control over the transport — for example to install a custom
`SocketsHttpHandler.ConnectCallback` that enforces per-hop SSRF invariants, or to add
an auth-refreshing `DelegatingHandler` — use `ConfigureOllamaHttpClient(...)` to get
the underlying `IHttpClientBuilder`:

```csharp
services.AddOllamaClient(o => { ... });

services.ConfigureOllamaHttpClient()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        ConnectCallback = MyAllowListedConnect,
    });
```

The same builder is returned for named clients:
`services.ConfigureOllamaHttpClient("analytics")`.

### Observability &amp; the `Authorization` header

This package never attaches the request `Authorization` header (or the raw `ApiKey`)
to any ActivitySource or Meter tag it emits. If you enable the stock
`AddHttpClientInstrumentation()` from `OpenTelemetry.Instrumentation.Http` over the
same `HttpClient`, the default configuration also omits request headers; if you
have opted into header capture via `enrichWithHttpRequestMessage` make sure to
redact/strip `Authorization` in your enricher.

### Token counting (not provided)

Ollama's REST API has no token-counting endpoint — the only official token counts
are `prompt_eval_count` / `eval_count` returned on generate/chat responses. This
package deliberately ships no client-side tokenizer: per-family tokenizers would
pull a large data dependency and break the current AOT/trim story. If you need a
cheap estimator, call `GenerateAsync` with your prompt and `Options.NumPredict = 0`
and read `prompt_eval_count` from the response.

### From `appsettings.json`

```json
{
  "Ollama": {
    "BaseAddress":  "http://localhost:11434/",
    "DefaultModel": "llama3.2",
    "Timeout":      "00:02:00"
  }
}
```

```csharp
builder.Services.AddOllamaClient(builder.Configuration.GetSection("Ollama"));
```

### Ollama Cloud

```csharp
// Prefer reading the key from configuration / secret store, not source code.
builder.Services.AddOllamaCloudClient(apiKey: builder.Configuration["Ollama:ApiKey"]!);
```

Cloud error handling differs in two places:

- **429** → `OllamaRateLimitedException` (used for both per-second limits **and** hourly/daily quota caps). Inspect `RetryAfter`.
- **402** → `OllamaQuotaExceededException` (subscription exhausted — retrying will not help).

## Error handling

The library throws narrow typed exceptions so you can `catch` the exact
scenario you care about:

| Exception | When |
|-----------|------|
| `OllamaConfigurationException` | Invalid client configuration |
| `OllamaConnectionException` | Cannot connect to server |
| `OllamaTimeoutException` | Request timeout |
| `OllamaAuthenticationException` | 401 Unauthorized |
| `OllamaAuthorizationException` | 403 Forbidden |
| `OllamaModelNotFoundException` | Model not found (404) |
| `OllamaModelPullRequiredException` | Model needs to be pulled first |
| `OllamaRequestValidationException` | Invalid request (400) |
| `OllamaPayloadTooLargeException` | Request/context too large |
| `OllamaRateLimitedException` | Rate limited (429) — also hourly/daily cloud quotas |
| `OllamaQuotaExceededException` | Cloud subscription quota exceeded (402) |
| `OllamaServerException` | Server error (500) — check `IsOutOfMemory` / `IsDiskFull` |
| `OllamaServiceUnavailableException` | 503 Service Unavailable |
| `OllamaStreamException` | Streaming failure — check `IsTruncated` |
| `OllamaDeserializationException` | JSON parsing failed |
| `OllamaApiException` | Generic API error with status code |

## Cancellation & timeouts

All async methods accept a `CancellationToken`:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var response = await client.Generation.GenerateAsync(request, cts.Token);
```

- **Non-streaming** requests enforce `OllamaClientOptions.Timeout` automatically.
- **Streaming** requests respect the caller's `CancellationToken` only.

## Observability

- **ActivitySource:** `Ollama.Net`
- **Meter:** `Ollama.Net`
- Metrics: `ollama.requests.total`, `ollama.requests.failed`, `ollama.request.duration` (histogram), `ollama.tokens.generated`.
- Activity tags: `ollama.endpoint`, `ollama.model`, `ollama.stream`, `ollama.status_code`, `ollama.duration_ms`.

## Security notes

- **HTTPS by default** — HTTP to non-loopback is rejected unless `AllowInsecureHttp = true`.
- **No credential storage** — keys are never persisted by the client.
- **No cookies** — the underlying `HttpClient` disables them to prevent session reuse.

## Links

- 📖 [Full README, samples & docs](https://github.com/chethandvg/Ollama.Net)
- 📝 [CHANGELOG](https://github.com/chethandvg/Ollama.Net/blob/main/CHANGELOG.md)
- 🤝 [CONTRIBUTING](https://github.com/chethandvg/Ollama.Net/blob/main/CONTRIBUTING.md)
- 🛡️ [SECURITY](https://github.com/chethandvg/Ollama.Net/blob/main/SECURITY.md)

## License

MIT © Chethan — see [LICENSE](https://github.com/chethandvg/Ollama.Net/blob/main/LICENSE).
