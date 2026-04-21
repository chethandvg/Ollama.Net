# Security Policy

## Supported versions

Only the latest published `Ollama.Net` minor version receives security fixes.
Older minor versions may receive a patch on a best-effort basis if the fix is
trivial.

| Version | Supported |
|---------|-----------|
| Latest `1.x` | ✅ |
| Older `1.x` | ⚠️ best-effort |
| `< 1.0`     | ❌ |

## Reporting a vulnerability

**Please do not open a public GitHub issue for security vulnerabilities.**

Use GitHub's private vulnerability reporting instead:

1. Go to <https://github.com/chethandvg/Ollama.Net/security/advisories/new>.
2. Fill in a clear description, reproduction steps, affected versions, and
   (if known) the impact and a suggested fix.
3. You will receive an acknowledgement within **72 hours**.

A fix will be prepared privately and released as a patch version. You will be
credited in the advisory unless you request otherwise.

## Scope

In scope:

- The `Ollama.Net` NuGet package and its source code in this repository.
- Transitive vulnerabilities caused by pinned dependency versions in
  `Directory.Packages.props`.

Out of scope:

- Vulnerabilities in the upstream [Ollama](https://ollama.com) server — report
  those to the Ollama project.
- Vulnerabilities in third-party packages that do not affect how `Ollama.Net`
  uses them.
