# Krutaka.Ollama Samples

Five self-contained samples that demonstrate how to wire `Krutaka.Ollama` into a typical .NET host, bound entirely from `appsettings.json` so you can see every knob a real app would use.

## Run a sample

```bash
# Local Ollama (default)
dotnet run --project samples/Krutaka.Ollama.Samples -- quickstart

# Ollama Cloud — set your key once, then switch profiles via DOTNET_ENVIRONMENT
export Ollama__ApiKey="sk-your-key"
DOTNET_ENVIRONMENT=Cloud dotnet run --project samples/Krutaka.Ollama.Samples -- quickstart
```

## Samples

| Name | Description |
|------|-------------|
| `quickstart` | Single `GenerateAsync` call. |
| `streaming` | `ChatStreamAsync` with a chunk cap (`Samples:StreamingMaxChunks`). |
| `embeddings` | `EmbedAsync` + pairwise cosine similarity. |
| `models` | `ListModelsAsync`, `ShowModelAsync`, `ListRunningModelsAsync`, optional streaming pull (set `PULL_MODEL=1`). |
| `toolcalling` | Function-calling round-trip with a stub `get_current_weather` tool. |

## Configuration

Resolution order (highest wins):

1. Command-line args (standard .NET host: `--Ollama:BaseAddress=...`).
2. Environment variables — structured (`Ollama__ApiKey`, `Samples__ChatModel`) or shorthand (`OLLAMA_HOST`, `OLLAMA_MODEL`).
3. `appsettings.{DOTNET_ENVIRONMENT}.json` (e.g. `appsettings.Cloud.json`).
4. `appsettings.json` — the defaults, with every setting documented inline.

The two shipped profiles:

- **`appsettings.json`** — local Ollama (`http://localhost:11434/`), `llama3.2`, generous timeouts, retries enabled.
- **`appsettings.Cloud.json`** — Ollama Cloud (`https://ollama.com/`), `gpt-oss:120b-cloud`, longer timeout; expects `Ollama__ApiKey` in the environment.

### Key environment variables

| Variable | Purpose |
|----------|---------|
| `DOTNET_ENVIRONMENT` | Selects the `appsettings.<env>.json` overlay (`Cloud`, `Staging`, ...). |
| `OLLAMA_HOST` | Shorthand for `Ollama:BaseAddress`. |
| `OLLAMA_MODEL` | Shorthand for `Ollama:DefaultModel`. |
| `Ollama__ApiKey` | Bearer token for Ollama Cloud. |
| `Ollama__Timeout` | e.g. `00:02:00` for a 2-minute timeout. |
| `Samples__ChatModel` | Override chat/generate model without touching the default. |
| `PULL_MODEL` | Set to `1` to enable the streaming pull demo in the `models` sample. |

Never commit real API keys. Use environment variables, [`user-secrets`](https://learn.microsoft.com/aspnet/core/security/app-secrets), or your cloud secret manager in production.

## Exit codes

| Code | Meaning |
|------|---------|
| `0` | Sample completed successfully |
| `1` | Unknown sample name |
| `2` | Ollama server did not respond to ping |
| `3` | `OllamaConnectionException` — could not reach server |
| `4` | `OllamaModelNotFoundException` — pull the model first |
| `5` | Other `OllamaException` |
| `6` | `OllamaAuthenticationException` — check `Ollama__ApiKey` |
| `7` | `OllamaQuotaExceededException` — cloud subscription exhausted |
| `8` | `OllamaRateLimitedException` |
| `130` | Cancelled by the user (`Ctrl+C`) |
