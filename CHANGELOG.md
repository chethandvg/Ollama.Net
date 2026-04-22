# Changelog

All notable changes to **Ollama.Net** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/chethandvg/Ollama.Net/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/chethandvg/Ollama.Net/releases/tag/v0.1.0
