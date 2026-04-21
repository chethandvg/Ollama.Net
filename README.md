<div align="center">

# 🦙 Ollama.Net

**Modern, async, AOT-friendly .NET client for the [Ollama](https://ollama.com) REST API.**

*Generation · Chat · Streaming · Embeddings · Tool Calling · Model Management · Ollama Cloud*

[![NuGet](https://img.shields.io/nuget/v/Ollama.Net.svg?logo=nuget&label=NuGet)](https://www.nuget.org/packages/Ollama.Net)
[![Downloads](https://img.shields.io/nuget/dt/Ollama.Net.svg?logo=nuget&label=downloads)](https://www.nuget.org/packages/Ollama.Net)
[![CI](https://github.com/chethandvg/Ollama.Net/actions/workflows/ci.yml/badge.svg)](https://github.com/chethandvg/Ollama.Net/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4?logo=dotnet)](https://dotnet.microsoft.com)

</div>

---

## ✨ Why Ollama.Net?

| | |
|---|---|
| 🚀 **Idiomatic .NET** | Built around `IHttpClientFactory`, `IOptions<T>`, `ILogger<T>` and `CancellationToken` — it feels like `Azure.*` or `Microsoft.Extensions.*`, not a port. |
| 🪶 **AOT & trim friendly** | All JSON goes through `System.Text.Json` source generators. Ships `IsTrimmable=true` and `IsAotCompatible=true`. |
| 🌊 **First-class streaming** | `IAsyncEnumerable<T>` for generate / chat / pull / push / create; cancel cleanly with a token. |
| 🧰 **Tool calling** | Strongly-typed `FunctionDefinition` and `ToolCall` records — no stringly-typed JSON. |
| 📦 **Full model management** | `list`, `show`, `pull`, `push`, `create`, `delete`, `copy`, `ps`, plus blob upload. |
| ☁️ **Ollama Cloud ready** | One-liner `AddOllamaCloudClient(apiKey: ...)` with proper `429` / `402` handling. |
| 🛡️ **Typed errors** | 15+ exceptions (`OllamaModelNotFoundException`, `OllamaRateLimitedException`, …) so `catch` blocks stay narrow. |
| 📊 **Observability built-in** | OpenTelemetry `ActivitySource` + `Meter` named `Ollama.Net`. |
| 🔁 **Resilient by default** | Automatic retries via `Microsoft.Extensions.Http.Resilience`. |
| 🔐 **Secure by default** | HTTP to non-loopback is rejected unless you opt in. No cookies, no credential storage. |

---

## 📦 Installation

```bash
dotnet add package Ollama.Net
```

Supported runtimes: **.NET 8**, **.NET 9**, **.NET 10**.

You also need a running Ollama instance — either locally
([install](https://ollama.com/download)) or [Ollama Cloud](https://ollama.com/cloud).

```bash
ollama serve              # terminal 1
ollama pull llama3.2      # terminal 2
```

---

## 🚀 Quick Start

### 1 · Register the client

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

### 2 · Generate text

```csharp
using Microsoft.Extensions.DependencyInjection;
using Ollama.Net.Abstractions;
using Ollama.Net.Models.Requests;

var client = host.Services.GetRequiredService<IOllamaClient>();

var response = await client.Generation.GenerateAsync(
    new GenerateRequest(Model: "llama3.2", Prompt: "Why is the sky blue?"));

Console.WriteLine(response.Response);
```

### 3 · Stream a chat

```csharp
using Ollama.Net.Models.Common;

var request = new ChatRequest(
    Model: "llama3.2",
    Messages: [ new OllamaMessage(OllamaRole.User, "Tell me a joke about C#.") ]);

await foreach (var chunk in client.Generation.ChatStreamAsync(request))
    Console.Write(chunk.Message.Content);
```

---

## 🧩 Examples

<details>
<summary><b>🔢 Embeddings</b></summary>

```csharp
var result = await client.Embeddings.EmbedAsync(
    new EmbedRequest(Model: "nomic-embed-text",
                     Input: ["The quick brown fox", "jumps over the lazy dog"]));

foreach (var vector in result.Embeddings)
    Console.WriteLine($"dim={vector.Length}");
```

</details>

<details>
<summary><b>🛠️ Tool calling</b></summary>

```csharp
var tool = new ToolDefinition(
    Type: "function",
    Function: new FunctionDefinition(
        Name: "get_current_weather",
        Description: "Get the current weather for a city.",
        Parameters: JsonDocument.Parse("""
            {
              "type":"object",
              "properties":{ "city":{"type":"string"} },
              "required":["city"]
            }""").RootElement));

var chat = await client.Generation.ChatAsync(new ChatRequest(
    Model: "llama3.2",
    Messages: [ new OllamaMessage(OllamaRole.User, "What's the weather in Tokyo?") ],
    Tools: [ tool ]));

foreach (var call in chat.Message.ToolCalls ?? [])
    Console.WriteLine($"→ {call.Function.Name}({call.Function.Arguments})");
```

</details>

<details>
<summary><b>📦 Model management</b></summary>

```csharp
// List
var models = await client.Models.ListModelsAsync();

// Pull with live progress
await foreach (var p in client.Models.PullModelStreamAsync(new PullModelRequest("llama3.2")))
    Console.WriteLine($"{p.Status}: {p.Completed}/{p.Total}");

// Show / delete / copy
await client.Models.ShowModelAsync  (new ShowModelRequest("llama3.2"));
await client.Models.DeleteModelAsync(new DeleteModelRequest("old-model"));
await client.Models.CopyModelAsync  (new CopyModelRequest(Source: "llama3.2", Destination: "my-llama"));
```

</details>

<details>
<summary><b>☁️ Ollama Cloud</b></summary>

```csharp
// Read the key from configuration or a secret store — never hard-code it.
builder.Services.AddOllamaCloudClient(apiKey: builder.Configuration["Ollama:ApiKey"]!);
```

Or via `appsettings.json`:

```json
{
  "Ollama": {
    "BaseAddress":   "https://ollama.com/",
    "ApiKey":        "sk-...",
    "DefaultModel":  "gpt-oss:120b-cloud"
  }
}
```

Ollama Cloud throws two extra exceptions you may want to catch:

- `OllamaRateLimitedException` → check `RetryAfter`, back off, retry.
- `OllamaQuotaExceededException` → subscription exhausted; retry **will not** help.

</details>

<details>
<summary><b>⚙️ Configuration from <code>appsettings.json</code></b></summary>

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

| Option | Default | Description |
|---|---|---|
| `BaseAddress` | `http://localhost:11434/` | Ollama server URL. |
| `DefaultModel` | `null` | Default model name (optional). |
| `Timeout` | `100s` | Non-streaming request timeout. |
| `MaxRetries` | `2` | Automatic retries (0–10). |
| `KeepAlive` | `5min` | How long the server keeps the model resident. |
| `UserAgent` | `"Ollama.Net"` | Sent as the `User-Agent` header. |
| `AuthorizationHeader` | `null` | Raw `Authorization` header (takes precedence over `ApiKey`). |
| `ApiKey` | `null` | Shortcut for `Authorization: Bearer {ApiKey}` — use for Ollama Cloud. |
| `AllowInsecureHttp` | `false` | Allow HTTP to non-loopback addresses. |

</details>

<details>
<summary><b>🧯 Error handling</b></summary>

```csharp
try
{
    var r = await client.Generation.GenerateAsync(request, ct);
}
catch (OllamaModelNotFoundException ex)   { /* pull it first  */ }
catch (OllamaTimeoutException)            { /* slow/overloaded */ }
catch (OllamaRateLimitedException ex)     { /* back off RetryAfter */ }
catch (OllamaServerException ex) when (ex.IsOutOfMemory) { /* smaller model */ }
```

Full list: `OllamaConfiguration/Connection/Timeout/Authentication/Authorization/ModelNotFound/ModelPullRequired/RequestValidation/PayloadTooLarge/RateLimited/QuotaExceeded/Server/ServiceUnavailable/Stream/Deserialization/Api`Exception.

</details>

<details>
<summary><b>📈 Observability</b></summary>

- **ActivitySource:** `Ollama.Net`
- **Meter:** `Ollama.Net`

Metrics:

- `ollama.requests.total`
- `ollama.requests.failed`
- `ollama.request.duration` (histogram, ms)
- `ollama.tokens.generated`

Wire it into your OpenTelemetry pipeline like any other `ActivitySource`/`Meter`.

</details>

---

## 🗂️ Repository layout

```
Ollama.Net/
├── src/Ollama.Net/                # The library (→ NuGet package)
├── tests/Ollama.Net.Tests/        # xUnit + FluentAssertions + WireMock.Net
├── samples/Ollama.Net.Samples/    # 5 runnable samples
├── docs/
│   ├── PUBLISHING-AND-MAINTENANCE.md  # End-to-end publish + maintain playbook
│   ├── VERSIONING.md                  # MinVer guide, beginner-friendly
│   └── PRE-PUBLISH-CHECKLIST.md       # One-off steps before 1.0.0
├── .github/workflows/             # CI + release pipelines
├── Directory.Build.props          # Repo-wide MSBuild defaults
├── Directory.Packages.props       # Central package versions
├── CHANGELOG.md                   # Keep a Changelog
└── Ollama.Net.sln
```

## 🧪 Build & test locally

```bash
dotnet restore
dotnet build -c Release
dotnet test  -c Release
```

All three must succeed with **0 warnings and 0 errors** before you open a PR.

## 📚 Documentation

- [`docs/PUBLISHING-AND-MAINTENANCE.md`](docs/PUBLISHING-AND-MAINTENANCE.md) —
  **start here if you're about to publish**: the full step-by-step playbook
  for testing, releasing `v1.0.0`, and maintaining the package long-term.
- [`docs/VERSIONING.md`](docs/VERSIONING.md) — beginner-friendly guide to
  releasing with **MinVer**.
- [`docs/PRE-PUBLISH-CHECKLIST.md`](docs/PRE-PUBLISH-CHECKLIST.md) — one-off
  checklist of GitHub/NuGet settings to tick before the first tag.
- [`CONTRIBUTING.md`](CONTRIBUTING.md) · [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md) · [`SECURITY.md`](SECURITY.md)
- [`CHANGELOG.md`](CHANGELOG.md)
- [`OLLAMA-NUGET-PUBLISHING.md`](OLLAMA-NUGET-PUBLISHING.md) — the original
  full migration + publishing playbook.

## 🗺️ Roadmap

- [ ] Publish `v1.0.0` to NuGet.org.
- [ ] Enable `EnablePackageValidation` with a `PackageValidationBaselineVersion`.
- [ ] Opt in to [NuGet trusted publishing via OIDC](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing-for-nuget-org)
      and remove the long-lived API key.
- [ ] Optional code-signed assemblies for enterprise consumers.

## 🤝 Contributing

PRs are warmly welcomed! Please read
[CONTRIBUTING.md](CONTRIBUTING.md) and
[CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) first, and open an issue for
anything beyond a typo-fix.

## 📝 License

[MIT](LICENSE) © Chethan
