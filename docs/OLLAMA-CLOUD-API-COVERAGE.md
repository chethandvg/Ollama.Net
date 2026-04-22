# Ollama (Cloud) API — Feature Coverage in `Ollama.Net`

> **Status date:** 2026-04-22
> **Sources (public Ollama docs / repo):**
> - <https://docs.ollama.com/api> (canonical API reference — moving target)
> - <https://github.com/ollama/ollama/blob/main/docs/api.md> (mirror / historical)
> - <https://docs.ollama.com/cloud> (Ollama Cloud overview)
> - <https://ollama.com/cloud> (model catalogue & pricing)
> - <https://ollama.com/settings> (API keys)
>
> **Last implementation update:** 2026-04-22 — structured outputs (schema objects), thinking-model support, and the nine missing `options` knobs are now covered, plus an `Extra` escape hatch for future options.
>
> **Scope:** The same REST surface is exposed by local `ollama serve` and by **Ollama Cloud** (`https://ollama.com/`). Cloud adds only three things on top of the local API: (1) a different base URL, (2) bearer-token authentication, and (3) two extra HTTP status codes (`402` quota, `429` rate limit). Cloud-hosted models are addressed with a `-cloud` suffix on the model name (e.g. `gpt-oss:120b-cloud`, `qwen3-coder:480b-cloud`). Everything else — request bodies, response shapes, streaming semantics — is identical.
>
> This document therefore enumerates the **entire Ollama REST API** and, for each feature, marks whether `Ollama.Net` implements it today.

## Legend

| Symbol | Meaning |
|---|---|
| ✅ | Fully implemented and exposed on the public client surface. |
| ⚠️ | Partially implemented / reachable with caveats (see notes). |
| ❌ | Not implemented. |

---

## 1. Connectivity & authentication (Ollama Cloud specifics)

| Feature | Ollama API | `Ollama.Net` | Notes / citations |
|---|---|---|---|
| Cloud base URL `https://ollama.com/` | Required for Cloud | ✅ | `OllamaServiceCollectionExtensions.OllamaCloudBaseAddress` (`src/Ollama.Net/DependencyInjection/OllamaServiceCollectionExtensions.cs:50`). |
| One-liner cloud registration | — | ✅ | `AddOllamaCloudClient(apiKey, …)` and named overload `AddOllamaCloudClient(name, apiKey, …)`. |
| Bearer-token auth (`Authorization: Bearer <key>`) | Required for Cloud | ✅ | `OllamaClientOptions.ApiKey` → bearer header; `OllamaClientOptions.AuthorizationHeader` overrides it verbatim. |
| Configurable timeout / keep-alive / retries / User-Agent | General | ✅ | `OllamaClientOptions` (`Timeout`, `KeepAlive`, `MaxRetries`, `UserAgent`). |
| Automatic retry on transient 5xx / network errors | General | ✅ | `Microsoft.Extensions.Http.Resilience` standard pipeline; retries deliberately disabled for streaming requests (detected via `X-Ollama-Stream` header). |
| Reject plaintext HTTP to non-loopback unless opted in | Security | ✅ | `OllamaClientOptions.AllowInsecureHttp` (default `false`). |
| `429 Too Many Requests` → typed exception with `Retry-After` | Cloud-only in practice | ✅ | `OllamaRateLimitedException`. |
| `402 Payment Required` / quota exceeded → typed exception | Cloud-only | ✅ | `OllamaQuotaExceededException`. |
| `401` / `403` → typed exceptions | General | ✅ | `OllamaAuthenticationException`, `OllamaAuthorizationException`. |
| OpenTelemetry traces + metrics | Client-side convenience | ✅ | `ActivitySource` / `Meter` named `Ollama.Net` (see README). |
| Named / keyed clients (multi-tenant, local + cloud in same app) | Client-side convenience | ✅ | `IOllamaClientFactory`, keyed DI. |
| Cloud-model discovery via `/api/tags` | Works identically against cloud | ✅ | Uses `ListModelsAsync`. Cloud returns only the subset of models attached to the account. |

---

## 2. Endpoint coverage

All endpoints listed here are the same for local and cloud deployments.

| HTTP | Path | Purpose | `Ollama.Net` | Where |
|---|---|---|---|---|
| `POST` | `/api/generate` | Single-prompt completion (streaming default) | ✅ | `IOllamaGenerationClient.GenerateAsync` / `GenerateStreamAsync` |
| `POST` | `/api/chat` | Multi-turn chat completion | ✅ | `IOllamaGenerationClient.ChatAsync` / `ChatStreamAsync` |
| `POST` | `/api/embed` | Batch embeddings | ✅ | `IOllamaEmbeddingsClient.EmbedAsync` |
| `POST` | `/api/embeddings` *(legacy, deprecated)* | Single-input embedding | ✅ | `IOllamaEmbeddingsClient.EmbedLegacyAsync` *(marked `[Obsolete]`)* |
| `GET`  | `/api/tags` | List local / account-available models | ✅ | `IOllamaModelsClient.ListModelsAsync` |
| `POST` | `/api/show` | Show model metadata / template / params | ✅ | `IOllamaModelsClient.ShowModelAsync` (supports `verbose`) |
| `POST` | `/api/pull` | Download a model (streaming progress) | ✅ | `PullModelAsync` / `PullModelStreamAsync` |
| `POST` | `/api/push` | Upload a model to a registry | ✅ | `PushModelAsync` / `PushModelStreamAsync` |
| `POST` | `/api/create` | Create a model from a base + Modelfile fragments | ✅ | `CreateModelAsync` / `CreateModelStreamAsync` |
| `DELETE` | `/api/delete` | Delete a model | ✅ | `DeleteModelAsync` |
| `POST` | `/api/copy` | Rename / copy a model | ✅ | `CopyModelAsync` |
| `GET`  | `/api/ps` | List running (loaded-in-VRAM) models | ✅ | `ListRunningModelsAsync` |
| `GET`  | `/api/version` | Server version string | ✅ | `IOllamaSystemClient.GetVersionAsync` |
| `HEAD` | `/` | Reachability probe (convenience, not a documented API) | ✅ | `IOllamaSystemClient.PingAsync` |
| `HEAD` | `/api/blobs/:digest` | Check whether a blob is already on the server | ✅ | `IOllamaSystemClient.BlobExistsAsync` (validates `sha256:<64-hex>`) |
| `POST` | `/api/blobs/:digest` | Upload a blob (used before `create` for `files`/`adapters`) | ✅ | `IOllamaSystemClient.PushBlobAsync` (streams the request body) |
| `POST` | `/api/generate` (*experimental* image generation params `width`/`height`/`steps`) | Diffusion-style image gen for image-gen models | ❌ | Not modelled on `GenerateRequest`. See §6. |
| `POST` | `/v1/chat/completions`, `/v1/completions`, `/v1/embeddings`, `/v1/models` (OpenAI-compat shim) | Drop-in OpenAI SDK compatibility | ❌ | Not exposed — users who want the OpenAI shape should use the OpenAI SDK with `BaseAddress = https://ollama.com/v1/`. See §6. |

---

## 3. `POST /api/generate` — request parameters

| Parameter | Ollama API | `Ollama.Net` (`GenerateRequest`) | Notes |
|---|---|---|---|
| `model` | required | ✅ `Model` | |
| `prompt` | required | ✅ `Prompt` | |
| `suffix` | optional | ✅ `Suffix` | |
| `system` | optional | ✅ `System` | |
| `template` | optional | ✅ `Template` | |
| `context` (deprecated) | optional | ✅ `Context` | Kept for compatibility; Ollama has marked it deprecated in favour of `/api/chat`. |
| `raw` | optional | ✅ `Raw` | |
| `stream` | optional | ✅ `Stream` | Auto-set by `GenerateAsync` vs `GenerateStreamAsync`. |
| `keep_alive` | optional | ✅ `KeepAlive` | |
| `images` (base64, for multimodal) | optional | ✅ `Images` | |
| `format` = `"json"` (JSON mode) | optional | ✅ `Format` | Pass `"json"`. |
| `format` = JSON-schema object (structured outputs) | optional | ⚠️ `Format` | `Format` is typed as `string?`, so a schema has to be supplied as a **raw JSON string** of the schema. There is no strongly-typed schema builder and the JSON wire-shape for `format` is `string` only (not `object`). See §6.1. |
| `options` (sampling / runtime knobs) | optional | ⚠️ `OllamaOptions` | Subset only — see §5. |
| `think` (thinking-capable models) | optional | ❌ | Not on `GenerateRequest`. See §6.2. |
| Image-gen: `width`, `height`, `steps` *(experimental)* | optional | ❌ | Not modelled. See §6.3. |

---

## 4. `POST /api/chat` — request parameters

| Parameter | Ollama API | `Ollama.Net` (`ChatRequest`) | Notes |
|---|---|---|---|
| `model` | required | ✅ `Model` | |
| `messages[]` | required | ✅ `Messages` | |
| &nbsp;&nbsp;`.role` (`system`/`user`/`assistant`/`tool`) | required | ✅ `OllamaRole` | Enum with all four roles. |
| &nbsp;&nbsp;`.content` | required | ✅ `Content` | |
| &nbsp;&nbsp;`.images` (base64) | optional | ✅ `Images` | |
| &nbsp;&nbsp;`.tool_calls` | assistant messages | ✅ `ToolCalls` | Strongly typed `ToolCall` / `ToolCallFunction`. |
| &nbsp;&nbsp;`.tool_name` (for `tool` role replies) | optional | ✅ `ToolName` | |
| &nbsp;&nbsp;`.thinking` (thinking-capable models) | optional | ✅ `Thinking` | Populated on response messages when `Think = true`; preserved across streamed chunks. |
| `tools[]` (function-calling) | optional | ✅ `Tools` | `ToolDefinition` + `FunctionDefinition` with `JsonElement Parameters` (JSON-schema). |
| `format` = `"json"` | optional | ✅ `Format` | |
| `format` = JSON-schema object (structured outputs) | optional | ✅ `Format` | Same `OllamaFormat` type as on `/api/generate`. |
| `options` | optional | ✅ `OllamaOptions` | See §5. |
| `stream` | optional | ✅ `Stream` | Auto-set by `ChatAsync` vs `ChatStreamAsync`. |
| `keep_alive` | optional | ✅ `KeepAlive` | |
| `think` (top-level toggle for thinking models) | optional | ✅ `Think` | Bool on `ChatRequest`. |

---

## 5. `options` object (Modelfile runtime parameters)

These are the parameters Ollama accepts inside the `"options"` bag on `/api/generate`, `/api/chat` and `/api/embed`.

| Option | Ollama API | `Ollama.Net` (`OllamaOptions`) |
|---|---|---|
| `temperature` | ✅ | ✅ `Temperature` |
| `top_p` | ✅ | ✅ `TopP` |
| `top_k` | ✅ | ✅ `TopK` |
| `num_ctx` | ✅ | ✅ `NumCtx` |
| `num_predict` | ✅ | ✅ `NumPredict` |
| `stop` | ✅ | ✅ `Stop` |
| `seed` | ✅ | ✅ `Seed` |
| `repeat_penalty` | ✅ | ✅ `RepeatPenalty` |
| `mirostat` | ✅ | ✅ `MirostatMode` |
| `mirostat_tau` | ✅ | ✅ `MirostatTau` |
| `mirostat_eta` | ✅ | ✅ `MirostatEta` |
| `presence_penalty` | ✅ | ✅ `PresencePenalty` |
| `frequency_penalty` | ✅ | ✅ `FrequencyPenalty` |
| `num_gpu` | ✅ | ✅ `NumGpu` |
| `num_thread` | ✅ | ✅ `NumThread` |
| `min_p` | ✅ | ✅ `MinP` |
| `typical_p` | ✅ | ✅ `TypicalP` |
| `num_keep` | ✅ | ✅ `NumKeep` |
| `repeat_last_n` | ✅ | ✅ `RepeatLastN` |
| `penalize_newline` | ✅ | ✅ `PenalizeNewline` |
| `num_batch` | ✅ | ✅ `NumBatch` |
| `main_gpu` | ✅ | ✅ `MainGpu` |
| `use_mmap` | ✅ | ✅ `UseMmap` |
| `numa` | ✅ | ✅ `Numa` |
| Escape hatch for arbitrary unknown future options | — | ✅ `Extra` | `IReadOnlyDictionary<string, JsonElement>?` — entries are flattened into the serialised `options` object. Unknown keys on incoming JSON round-trip through `Extra` as well. |

> The `OllamaOptions` record also exposes a legacy `Format` property marked `[Obsolete]`; it predates the top-level `format` field and is **not** a documented options-bag key. Use the top-level `Format` on `GenerateRequest` / `ChatRequest` instead.

---

## 6. Response-shape coverage

### `/api/generate` → `GenerateResponse`

| Field | API | `Ollama.Net` |
|---|---|---|
| `model`, `created_at`, `response`, `done` | ✅ | ✅ |
| `done_reason` (`stop`, `length`, `unload`, …) | ✅ | ✅ `DoneReason` |
| `context` | ✅ | ✅ `Context` |
| `total_duration`, `load_duration`, `prompt_eval_count`, `prompt_eval_duration`, `eval_count`, `eval_duration` (all in **nanoseconds**) | ✅ | ✅ (exposed as `long`; nanoseconds are **not** converted to `TimeSpan`) |

### `/api/chat` → `ChatResponse`

| Field | API | `Ollama.Net` |
|---|---|---|
| `model`, `created_at`, `message`, `done`, `done_reason` | ✅ | ✅ |
| `message.content`, `message.tool_calls`, `message.role` | ✅ | ✅ |
| `message.thinking` (thinking-capable models) | ✅ | ✅ `Thinking` — round-trips on both streamed and non-streamed responses. |
| Timing / token counters | ✅ | ✅ |

### `/api/embed` → `EmbedResponse`

| Field | API | `Ollama.Net` |
|---|---|---|
| `embeddings[][]`, `model`, `total_duration`, `load_duration`, `prompt_eval_count` | ✅ | ✅ |

### Progress responses (`pull` / `push` / `create`)

| Field | API | `Ollama.Net` |
|---|---|---|
| `status`, `digest`, `total`, `completed` | ✅ | ✅ `ProgressResponse` |

### `/api/tags`, `/api/ps`, `/api/show`

| Response | API | `Ollama.Net` |
|---|---|---|
| `ModelList` / `RunningModelList` / `ShowModelResponse` / `VersionResponse` | ✅ | ✅ |

---

## 6. Remaining gaps

These are the only ❌ / ⚠️ rows that survive the 2026-04-22 implementation pass.

### 6.1 Experimental image-generation parameters

- `width`, `height`, `steps` are marked **experimental** in the upstream docs and may change. `Ollama.Net` tracks the stable surface and does not model them. If image-gen models become generally available on Cloud, a follow-up is needed.

### 6.2 OpenAI-compatible `/v1/*` routes

- Ollama exposes a partial OpenAI-compat shim (`/v1/chat/completions`, `/v1/completions`, `/v1/embeddings`, `/v1/models`) on both local and cloud. `Ollama.Net` intentionally targets the native `/api/*` surface (richer, more type-safe) and does not wrap the compat routes. This is an explicit design choice rather than a defect, and users who specifically need the OpenAI shape can use the OpenAI .NET SDK pointed at `https://ollama.com/v1/` with the same bearer token.

### 6.3 Nanosecond durations surfaced as `long`

- Timing fields in responses are nanoseconds (per the Ollama spec). `Ollama.Net` exposes them as `long`, not `TimeSpan`. Not a bug, but a minor ergonomic gap — consider a `Duration`-typed computed property (`TotalDuration.ToTimeSpan()`) in a future minor release.

---

## 7. Summary scorecard

| Area | Coverage |
|---|---|
| Cloud connectivity (base URL, bearer auth, 402/429 handling) | **100%** |
| Documented REST endpoints (`/api/*`) | **15 / 15** implemented |
| `/api/generate` request fields | **13 / 13** |
| `/api/chat` request fields + message sub-fields | **13 / 13** |
| `options` bag | **24 / 24** known knobs + forward-compat `Extra` escape hatch |
| Response fields on generate / chat / embed / progress / tags / ps / show / version | **100%** |
| Experimental image-gen params | **0 / 3** (intentional) |
| OpenAI-compat `/v1/*` shim | intentionally **not covered** |

**Bottom line:** the library now matches the full stable Ollama Cloud surface. The only remaining gaps are the explicitly-experimental image-generation parameters and the intentionally-unsupported `/v1/*` OpenAI-compat shim.
