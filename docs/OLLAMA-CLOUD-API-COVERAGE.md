# Ollama (Cloud) API — Feature Coverage in `Ollama.Net`

> **Status date:** 2026-04-22
> **Sources (public Ollama docs / repo):**
> - <https://docs.ollama.com/api> (canonical API reference — moving target)
> - <https://github.com/ollama/ollama/blob/main/docs/api.md> (mirror / historical)
> - <https://docs.ollama.com/cloud> (Ollama Cloud overview)
> - <https://ollama.com/cloud> (model catalogue & pricing)
> - <https://ollama.com/settings> (API keys)
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
| &nbsp;&nbsp;`.thinking` (thinking-capable models) | optional | ❌ | Not modelled on `OllamaMessage`. See §6.2. |
| `tools[]` (function-calling) | optional | ✅ `Tools` | `ToolDefinition` + `FunctionDefinition` with `JsonElement Parameters` (JSON-schema). |
| `format` = `"json"` | optional | ✅ `Format` | |
| `format` = JSON-schema object (structured outputs) | optional | ⚠️ `Format` | Same caveat as §3: typed as `string?`. |
| `options` | optional | ⚠️ `OllamaOptions` | See §5. |
| `stream` | optional | ✅ `Stream` | Auto-set by `ChatAsync` vs `ChatStreamAsync`. |
| `keep_alive` | optional | ✅ `KeepAlive` | |
| `think` (top-level toggle for thinking models) | optional | ❌ | See §6.2. |

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
| `min_p` | ✅ | ❌ |
| `typical_p` | ✅ | ❌ |
| `num_keep` | ✅ | ❌ |
| `repeat_last_n` | ✅ | ❌ |
| `penalize_newline` | ✅ | ❌ |
| `num_batch` | ✅ | ❌ |
| `main_gpu` | ✅ | ❌ |
| `use_mmap` | ✅ | ❌ |
| `numa` | ✅ | ❌ |
| Escape hatch for arbitrary unknown future options | — | ❌ | `OllamaOptions` is a sealed record with no extension bag; unknown knobs cannot be sent without a library change. See §6.4. |

> The `OllamaOptions` record also exposes a `Format` property that predates the top-level `format` field and is effectively redundant — prefer the top-level `Format` on `GenerateRequest` / `ChatRequest`.

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
| `message.thinking` (thinking-capable models) | ✅ | ❌ — no field on `OllamaMessage`; thinking output is silently dropped during deserialisation. See §6.2. |
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

## 6. Known gaps (what is **not** implemented today)

These are collected from the per-row ❌/⚠️ rows above and elevated here so they are easy to action.

### 6.1 Structured outputs with a JSON schema object

- The API accepts `format` as either the string `"json"` **or** a full JSON-schema object.
- `GenerateRequest.Format` / `ChatRequest.Format` are typed as `string?`, and the source-generated JSON context serialises it as a JSON string value, not a JSON object. That means callers cannot send a schema today without a library change. The README lists “Structured outputs” only implicitly via JSON mode.
- **Impact for Cloud:** Cloud models (especially larger ones such as `gpt-oss:120b-cloud`, `qwen3-coder:480b-cloud`) are the prime consumers of schema-constrained outputs, so this is the most noticeable gap for cloud users.

### 6.2 Thinking / reasoning models

- The API exposes:
  - A top-level `think: true|false` parameter on `/api/generate` and `/api/chat`.
  - A per-message `thinking` string field on response messages.
- Neither is present on `GenerateRequest`, `ChatRequest`, or `OllamaMessage`. Cloud hosts reasoning-capable models (e.g. `deepseek-v3.1:671b-cloud`, `gpt-oss:120b-cloud`), so their thinking traces cannot be read back through this library.

### 6.3 Experimental image-generation parameters

- `width`, `height`, `steps` are marked **experimental** in the upstream docs and may change. `Ollama.Net` tracks the stable surface and does not model them. If image-gen models become generally available on Cloud, a follow-up is needed.

### 6.4 Escape hatch for unknown `options`

- New Modelfile runtime options land in Ollama fairly often (`min_p`, `typical_p`, etc. were added post-1.0). Because `OllamaOptions` is a sealed record with no `IDictionary<string, JsonElement>` extension bag, any new option requires a library release. Consider adding either the missing fields enumerated in §5, an extensibility point, or both.

### 6.5 OpenAI-compatible `/v1/*` routes

- Ollama exposes a partial OpenAI-compat shim (`/v1/chat/completions`, `/v1/completions`, `/v1/embeddings`, `/v1/models`) on both local and cloud. `Ollama.Net` intentionally targets the native `/api/*` surface (richer, more type-safe) and does not wrap the compat routes. This is an explicit design choice rather than a defect, and users who specifically need the OpenAI shape can use the OpenAI .NET SDK pointed at `https://ollama.com/v1/` with the same bearer token.

### 6.6 Nanosecond durations surfaced as `long`

- Timing fields in responses are nanoseconds (per the Ollama spec). `Ollama.Net` exposes them as `long`, not `TimeSpan`. Not a bug, but a minor ergonomic gap — consider a `Duration`-typed computed property (`TotalDuration.ToTimeSpan()`) in a future minor release.

---

## 7. Summary scorecard

| Area | Coverage |
|---|---|
| Cloud connectivity (base URL, bearer auth, 402/429 handling) | **100%** |
| Documented REST endpoints (`/api/*`) | **15 / 15** implemented |
| `/api/generate` request fields | **11 / 13** (missing `think`, structured-output schema object) |
| `/api/chat` request fields + message sub-fields | **11 / 13** (missing `think`, `message.thinking`) |
| `options` bag | **15 / 24** (+ no extension escape hatch) |
| Response fields on generate / chat / embed / progress / tags / ps / show / version | **~95%** (only `message.thinking` missing) |
| Experimental image-gen params | **0 / 3** |
| OpenAI-compat `/v1/*` shim | intentionally **not covered** |

**Bottom line:** every mainstream Ollama Cloud workflow — chat, generate, streaming, tool calling, embeddings, model management, blob upload, and the cloud-specific auth / rate-limit / quota paths — is covered. The meaningful functional gaps are (1) **structured outputs via a schema object** and (2) **thinking-model support (`think` parameter + `message.thinking`)**, with secondary gaps in the less-used `options` knobs, image-generation experimentals, and an extensibility escape hatch.
