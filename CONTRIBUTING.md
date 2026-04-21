# Contributing to Ollama.Net

Thanks for thinking about contributing! This project aims to stay small,
well-tested, and pleasant to use. These rules keep the codebase consistent and
the review loop fast.

## Before you start

1. **Open an issue first.** For anything larger than a typo, open a GitHub
   issue describing the bug or the feature. This avoids duplicated work and
   lets us agree on the design before you invest time.
2. **Claim it.** Comment on the issue so others know you're on it.
3. **Fork + branch.** Use a descriptive branch name, e.g.
   `fix/stream-reader-eof` or `feat/chat-tool-choice`.

## Local setup

Prerequisites:

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (the build also targets
  `net8.0` and `net9.0`, but the SDK stays forward-compatible).
- `git`.

```bash
git clone https://github.com/chethandvg/Ollama.Net.git
cd Ollama.Net
dotnet restore
dotnet build -c Release
dotnet test  -c Release
```

All three must succeed with **0 warnings and 0 errors** before you push.

## Coding rules

- Follow the `.editorconfig` at the repo root — your IDE will pick it up
  automatically.
- Nullable reference types are **on**; do not silence warnings with `!` unless
  you can justify it in a comment.
- Every public API must have XML doc comments (`/// <summary>...</summary>`).
- Add tests for every new behaviour. Unit tests live in
  `tests/Ollama.Net.Tests/` and use **xUnit + FluentAssertions + NSubstitute +
  WireMock.Net**.
- Preserve AOT/trim-friendliness: no runtime reflection-based serialization;
  new DTOs must be registered in `OllamaJsonContext`.
- Keep the public API surface additive where possible. Breaking changes
  require a major version bump and a CHANGELOG entry in the `[Unreleased]`
  section.

## Commit style

- One logical change per commit.
- Subject line ≤ 72 chars, imperative mood (`Fix streaming EOF handling`,
  not `Fixed` or `Fixes`).
- Reference the issue number in the body: `Closes #42.`

## Pull request checklist

- [ ] `dotnet build -c Release` succeeds with 0 warnings.
- [ ] `dotnet test -c Release` succeeds with 0 failures.
- [ ] New or changed behaviour is covered by tests.
- [ ] Public API changes are documented in `CHANGELOG.md` under `[Unreleased]`.
- [ ] README / XML docs updated if relevant.

## Releasing (maintainers only)

Releases are driven by git tags. See [`docs/VERSIONING.md`](docs/VERSIONING.md)
for the full playbook.

## Code of Conduct

By participating you agree to abide by the
[Code of Conduct](CODE_OF_CONDUCT.md).
