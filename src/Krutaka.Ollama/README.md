# Krutaka.Ollama

Modern, async, AOT-friendly .NET client for the [Ollama](https://ollama.ai/) REST API. Supports text generation, chat, embeddings, model management, streaming responses, and tool calling.

## Features

- ✅ **Fully async/await** with `ConfigureAwait(false)` throughout
- ✅ **AOT and trimming compatible** using System.Text.Json source generators
- ✅ **Streaming support** for generation, chat, and model operations
- ✅ **Tool calling** with structured function definitions
- ✅ **Embeddings** with batch support
- ✅ **Model management** (pull, push, create, delete, copy, list, show)
- ✅ **System operations** (version, ping, blob management)
- ✅ **Comprehensive error handling** with 15+ exception types
- ✅ **Observability** with OpenTelemetry metrics and tracing
- ✅ **Named clients** via `IOllamaClientFactory` for multi-server scenarios
- ✅ **Resilience** with automatic retries via `Microsoft.Extensions.Http.Resilience`

## Installation

```bash
dotnet add package Krutaka.Ollama
```

## Quick Start

### Register with Dependency Injection

```csharp
using Krutaka.Ollama.DependencyInjection;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    services.AddOllamaClient(options =>
    {
        options.BaseAddress = new Uri("http://localhost:11434/");
        options.DefaultModel = "llama3.2";
        options.Timeout = TimeSpan.FromSeconds(120);
    });
});

var host = builder.Build();
```

### Generate Text

```csharp
using Krutaka.Ollama.Abstractions;
using Krutaka.Ollama.Models.Requests;

IOllamaClient client = host.Services.GetRequiredService<IOllamaClient>();

var request = new GenerateRequest(
    Model: "llama3.2",
    Prompt: "Why is the sky blue?"
);

GenerateResponse response = await client.Generation.GenerateAsync(request);
Console.WriteLine(response.Response);
```

### Streaming Chat

```csharp
using Krutaka.Ollama.Models.Common;

var chatRequest = new ChatRequest(
    Model: "llama3.2",
    Messages:
    [
        new OllamaMessage(OllamaRole.User, "Tell me a joke about programming")
    ]
);

await foreach (ChatResponse chunk in client.Generation.ChatStreamAsync(chatRequest))
{
    Console.Write(chunk.Message.Content);
}
```

### Generate Embeddings

```csharp
var embedRequest = new EmbedRequest(
    Model: "nomic-embed-text",
    Input: ["The quick brown fox", "jumps over the lazy dog"]
);

EmbedResponse embeddings = await client.Embeddings.EmbedAsync(embedRequest);

foreach (float[] embedding in embeddings.Embeddings)
{
    Console.WriteLine($"Embedding dimension: {embedding.Length}");
}
```

### Model Management

```csharp
// List all models
ModelList models = await client.Models.ListModelsAsync();

// Pull a model with progress
await foreach (ProgressResponse progress in client.Models.PullModelStreamAsync(
    new PullModelRequest("llama3.2")))
{
    Console.WriteLine($"{progress.Status}: {progress.Completed}/{progress.Total}");
}

// Show model details
ShowModelResponse details = await client.Models.ShowModelAsync(
    new ShowModelRequest("llama3.2"));

// Delete a model
await client.Models.DeleteModelAsync(new DeleteModelRequest("old-model"));
```

## Configuration Options

| Property | Default | Description |
|----------|---------|-------------|
| `BaseAddress` | `http://localhost:11434/` | Ollama server URL |
| `DefaultModel` | `null` | Default model name (optional) |
| `Timeout` | `100s` | Request timeout (non-streaming) |
| `MaxRetries` | `2` | Max retry attempts (0-10) |
| `KeepAlive` | `5min` | How long to keep models in memory |
| `UserAgent` | `"Krutaka.Ollama"` | User-Agent header |
| `AuthorizationHeader` | `null` | Raw `Authorization` header value (e.g., `"Bearer token"`). Takes precedence over `ApiKey`. |
| `ApiKey` | `null` | Shorthand for bearer-token auth. When set, the client sends `Authorization: Bearer {ApiKey}`. Use this for [Ollama Cloud](https://ollama.com/cloud). |
| `AllowInsecureHttp` | `false` | Allow HTTP to non-loopback (HTTPS recommended) |

### Configuration from appsettings.json

```json
{
  "Ollama": {
    "BaseAddress": "http://localhost:11434/",
    "DefaultModel": "llama3.2",
    "Timeout": "00:02:00"
  }
}
```

```csharp
services.AddOllamaClient(
    builder.Configuration.GetSection("Ollama"));
```

### Ollama Cloud

To talk to [Ollama Cloud](https://ollama.com/cloud), point the client at `https://ollama.com/` and supply a bearer token from <https://ollama.com/settings>. A shortcut is available:

```csharp
// Preferred: read the key from configuration or a secrets store, not source code.
services.AddOllamaCloudClient(apiKey: builder.Configuration["Ollama:ApiKey"]!);
```

Or via `appsettings.json`:

```json
{
  "Ollama": {
    "BaseAddress": "https://ollama.com/",
    "ApiKey": "sk-...",
    "DefaultModel": "gpt-oss:120b-cloud"
  }
}
```

Error handling for cloud differs in two places:

- **429 Too Many Requests** (`OllamaRateLimitedException`) is used for both transient per-second rate limits *and* hourly/daily quota caps. Inspect `Message` / `RetryAfter` to decide whether to back off and retry or surface to the user.
- **402 Payment Required** (`OllamaQuotaExceededException`) indicates the subscription plan is exhausted. Retrying will not help — the user must upgrade their plan or wait for the next billing period.

See [`samples/Krutaka.Ollama.Samples/appsettings.Cloud.json`](../../samples/Krutaka.Ollama.Samples/appsettings.Cloud.json) for a runnable cloud profile; activate it with `DOTNET_ENVIRONMENT=Cloud`.

## Error Handling

The library throws typed exceptions for different error scenarios:

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
| `OllamaPayloadTooLargeException` | Request/context too large (413 or 500 context) |
| `OllamaRateLimitedException` | Rate limited (429) — Ollama Cloud uses this for both per-second limits and hourly/daily quota |
| `OllamaQuotaExceededException` | Cloud subscription quota exceeded (402 Payment Required) |
| `OllamaServerException` | Server error (500) — check `IsOutOfMemory` / `IsDiskFull` |
| `OllamaServiceUnavailableException` | Service unavailable (503) |
| `OllamaStreamException` | Streaming error — check `IsTruncated`; also thrown when a model operation (pull/push/create) returns zero progress records |
| `OllamaDeserializationException` | JSON parsing failed |
| `OllamaApiException` | Generic API error with status code |

### Example

```csharp
try
{
    var response = await client.Generation.GenerateAsync(request);
}
catch (OllamaModelNotFoundException ex)
{
    Console.WriteLine($"Model '{ex.ModelName}' not found. Pull it first.");
}
catch (OllamaTimeoutException ex)
{
    Console.WriteLine($"Request timed out: {ex.Message}");
}
catch (OllamaServerException ex) when (ex.IsOutOfMemory)
{
    Console.WriteLine("Server ran out of memory. Try a smaller model.");
}
```

## Cancellation & Timeouts

All async methods accept a `CancellationToken`:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

var response = await client.Generation.GenerateAsync(request, cts.Token);
```

- **Non-streaming requests** enforce `OllamaClientOptions.Timeout` automatically.
- **Streaming requests** respect the user-provided `CancellationToken` only.

## Observability

The library emits OpenTelemetry metrics and traces:

- **ActivitySource**: `Krutaka.Ollama` (version `1.0.0`)
- **Meter**: `Krutaka.Ollama` (version `1.0.0`)

### Metrics

- `ollama.requests.total` — Total requests sent
- `ollama.requests.failed` — Failed requests
- `ollama.request.duration` (histogram) — Request duration in ms
- `ollama.tokens.generated` — Total tokens generated

### Tracing

Activities are tagged with:
- `ollama.endpoint` — API path
- `ollama.model` — Model name (if known)
- `ollama.stream` — Whether streaming
- `ollama.status_code` — HTTP status code
- `ollama.duration_ms` — Duration

## Security Notes

- **HTTPS by default**: HTTP connections to non-loopback addresses are rejected unless `AllowInsecureHttp = true`.
- **No credential storage**: The client never stores API keys or credentials; use `AuthorizationHeader` for custom auth.
- **No cookies**: The HTTP client disables cookies to prevent unintended session reuse.

## License

MIT — see [LICENSE](https://github.com/chethandvg/krutaka/blob/main/LICENSE) for details.

## Contributing

Contributions welcome! See [CONTRIBUTING.md](https://github.com/chethandvg/krutaka/blob/main/CONTRIBUTING.md).
