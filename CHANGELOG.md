# Changelog

All notable changes to **Ollama.Net** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0] — 2026-04-22

> **Breaking release.** `GenerateRequest.Format` / `ChatRequest.Format` change
> from `string?` to `OllamaFormat?`, and the positional primary constructors
> of the `OllamaMessage`, `OllamaOptions`, `GenerateRequest`, and `ChatRequest`
> records gain new parameters (binary-breaking). Call sites that use named
> arguments and `Format: "json"` continue to compile thanks to the implicit
> `string → OllamaFormat` conversion; consumers of the positional ctors /
> `Deconstruct` must update. See _Changed_ below.

### Fixed

- **`OllamaFormat` — `string?` → `OllamaFormat?` no longer throws.** The original
  implicit `string → OllamaFormat` conversion called `FromString`, which threw
  for `null` / empty inputs, breaking migration call sites like
  `string? format = condition ? "json" : null; new GenerateRequest(..., Format: format)`.
  A new null-tolerant overload `implicit operator OllamaFormat?(string?)` (plus
  the named alternate `OllamaFormat.FromStringOrNull`) maps `null` / empty back
  to `null`, so the `format` field is **omitted on the wire** — matching the
  legacy `string?`-based API semantics. `FromString` itself still throws when
  called explicitly with a `null` / empty argument.
- **`OllamaOptions.Extra` — reject key collisions with typed properties.** On
  write, `OllamaOptionsConverter` now throws `JsonException` if any entry in
  `Extra` uses a snake-case name already owned by a typed `OllamaOptions`
  property (e.g. `Extra["temperature"]` while `Temperature` is also set, or
  simply `Extra["num_predict"]`). This prevents the serialised `options`
  object from containing duplicate JSON members, whose first-wins vs last-wins
  semantics are undefined across parsers and could change the effective value
  depending on the server implementation.

### Added

- **Structured outputs** — new `OllamaFormat` union type (mode string or JSON-schema
  object) with implicit conversions from `string` and `JsonElement`. Schemas are
  serialised inline as JSON objects on the wire, unblocking the
  `format`-as-schema path documented by Ollama. `GenerateRequest.Format` and
  `ChatRequest.Format` now accept an `OllamaFormat?`.
- **Thinking-model support** — new `Think` bool on `GenerateRequest` and
  `ChatRequest`; new `Thinking` string on `OllamaMessage`. Works on both
  non-streamed and streamed responses. Enables reasoning-capable Ollama Cloud
  models (e.g. `gpt-oss:120b-cloud`, `deepseek-v3.1:671b-cloud`).
- **Nine previously-missing `OllamaOptions` knobs** — `MinP`, `TypicalP`,
  `NumKeep`, `RepeatLastN`, `PenalizeNewline`, `NumBatch`, `MainGpu`, `UseMmap`,
  `Numa`.
- **`OllamaOptions.Extra` escape hatch** (`IReadOnlyDictionary<string, JsonElement>?`)
  — arbitrary key/value pairs are flattened into the serialised `options`
  object so callers can forward future Ollama options without a library
  release. Backed by a hand-written AOT-safe `JsonConverter`; unknown keys on
  incoming JSON also round-trip through `Extra`.
- New documentation: `docs/OLLAMA-CLOUD-API-COVERAGE.md` mapping the full
  Ollama (Cloud) REST API against this library.

### Deprecated

- `OllamaOptions.Format` — not a documented options-bag key; use the top-level
  `GenerateRequest.Format` / `ChatRequest.Format` instead. Still serialised for
  binary compatibility with 0.1.0 consumers but will be removed in a future
  major release.

### Changed

- **[breaking]** `GenerateRequest.Format` and `ChatRequest.Format` changed from
  `string?` to `OllamaFormat?`. Call sites using `Format: "json"` continue to
  compile thanks to the implicit `string → OllamaFormat` conversion; code that
  read the property back as `string` needs to call `.AsMode()`.

## [1.0.3] — 2026-04-22

### Added

- Package icon displayed on NuGet.org search results.

## [1.0.2] — 2026-04-22

### Changed

- Enabled NuGet package validation against the `1.0.0` baseline to catch
  accidental breaking API changes.

## [1.0.1] — 2026-04-22

### Changed

- Internal release-pipeline fixes; no API changes.

## [1.0.0] — 2026-04-22

> First stable release of **`OllamaNet.Client`** (assembly / root namespace
> `Ollama.Net`). Same code as `Krutaka.Ollama 0.1.0`, published under a new
> package identity.

### Changed

- NuGet package ID set to **`OllamaNet.Client`** (assembly name and root
  namespace remain `Ollama.Net`). The short `Ollama.Net` ID is owned by
  another publisher on nuget.org (case-insensitively equal to `Ollama.NET`),
  so the distribution ships under `OllamaNet.Client`. Consumers install with
  `dotnet add package OllamaNet.Client` but code still uses
  `using Ollama.Net…`.
- Repository restructured for standalone NuGet publishing: the library, tests,
  and samples are now rooted at `Ollama.Net` (previously `Krutaka.Ollama`).
- Multi-targets `net8.0`, `net9.0`, and `net10.0`.
- Versioning moved to [MinVer](https://github.com/adamralph/minver) — package
  version is derived from the nearest `v*` git tag (see
  [`docs/VERSIONING.md`](docs/VERSIONING.md)).
- CI/CD pipelines added: `ci.yml` (build + test matrix, pack on `main`) and
  `release.yml` (tag-triggered publish to NuGet.org with symbols and a GitHub
  release).

## [0.1.0] — 2026-04-21

> Published as `Krutaka.Ollama 0.1.0` from the monorepo; same code, new
> package identity.

### Added

- Text generation (`GenerateAsync`, `GenerateStreamAsync`).
- Chat completion (`ChatAsync`, `ChatStreamAsync`).
- Embeddings (`EmbedAsync`, legacy `EmbedLegacyAsync`).
- Full model management:
  - List / show / list-running models.
  - Pull / push / create / delete / copy (with streaming variants for long-running ops).
- System operations: version, health ping, blob existence and upload.
- 15+ typed exception hierarchy for rich error handling.
- OpenTelemetry metrics and distributed tracing
  (`ActivitySource` / `Meter` named `Ollama.Net`).
- AOT + trimming friendly — all JSON handled by
  `System.Text.Json` source generators.
- Dependency-injection integration with named clients via
  `IOllamaClientFactory`.
- Automatic retries via `Microsoft.Extensions.Http.Resilience`.
- Security validation: HTTPS enforcement, timeout controls, no cookie storage.
- Snake-case JSON serialization matching Ollama API conventions.

### Security

- HTTP connections to non-loopback addresses are blocked by default.
- Request timeout enforced for non-streaming operations.
- No cookie or credential storage by default.
- Optional `AuthorizationHeader` / `ApiKey` for Ollama Cloud.

[Unreleased]: https://github.com/chethandvg/Ollama.Net/compare/v2.0.0...HEAD
[2.0.0]:      https://github.com/chethandvg/Ollama.Net/releases/tag/v2.0.0
[1.0.3]:      https://github.com/chethandvg/Ollama.Net/releases/tag/v1.0.3
[1.0.2]:      https://github.com/chethandvg/Ollama.Net/releases/tag/v1.0.2
[1.0.1]:      https://github.com/chethandvg/Ollama.Net/releases/tag/v1.0.1
[1.0.0]:      https://github.com/chethandvg/Ollama.Net/releases/tag/v1.0.0
[0.1.0]:      https://github.com/chethandvg/Ollama.Net/releases/tag/v0.1.0
