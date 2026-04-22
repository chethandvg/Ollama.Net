# Changelog

All notable changes to **Ollama.Net** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - 2026-04-21

### Added

- Initial release of Ollama.Net client library
- Text generation with `GenerateAsync` and `GenerateStreamAsync`
- Chat completion with `ChatAsync` and `ChatStreamAsync`
- Embeddings support via `EmbedAsync` and legacy `EmbedLegacyAsync`
- Full model management:
  - List models (`ListModelsAsync`)
  - Show model details (`ShowModelAsync`)
  - Pull models (`PullModelAsync`, `PullModelStreamAsync`)
  - Push models (`PushModelAsync`, `PushModelStreamAsync`)
  - Create models (`CreateModelAsync`, `CreateModelStreamAsync`)
  - Delete models (`DeleteModelAsync`)
  - Copy models (`CopyModelAsync`)
  - List running models (`ListRunningModelsAsync`)
- System operations:
  - Get version (`GetVersionAsync`)
  - Health check (`PingAsync`)
  - Blob management (`BlobExistsAsync`, `PushBlobAsync`)
- Comprehensive exception hierarchy with 15+ typed exceptions
- OpenTelemetry metrics and distributed tracing
- AOT and trimming support via System.Text.Json source generators
- Dependency injection with named clients via `IOllamaClientFactory`
- Automatic retry via `Microsoft.Extensions.Http.Resilience`
- Security validations (HTTPS enforcement, timeout controls)
- Snake_case JSON serialization matching Ollama API conventions

### Security

- HTTP connections to non-loopback addresses blocked by default
- Request timeout enforcement for non-streaming operations
- No cookie or credential storage by default
- Optional `AuthorizationHeader` for custom authentication

[Unreleased]: https://github.com/chethandvg/Ollama.Net/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/chethandvg/Ollama.Net/releases/tag/v0.1.0


## [1.0.0] — YYYY-MM-DD

_Copy the entries that were under `[Unreleased]` here._

[Unreleased]: https://github.com/chethandvg/Ollama.Net/compare/v1.0.0...HEAD
[1.0.0]:      https://github.com/chethandvg/Ollama.Net/releases/tag/v1.0.0