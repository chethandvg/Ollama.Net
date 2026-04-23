# 🚀 Release notes — `v2.1.0` post-merge steps

> Focused walkthrough for shipping the package-feedback enhancement release.
> For the generic release process, consult [`PUBLISHING-AND-MAINTENANCE.md`](PUBLISHING-AND-MAINTENANCE.md)
> and [`VERSIONING.md`](VERSIONING.md).

## Why `2.1.0` (and not `2.0.1` or `3.0.0`)?

| Change | SemVer bucket |
|---|---|
| New public API: `OllamaClientOptions.DisallowPrivateNetworks` | **MINOR** (additive) |
| New public API: `OllamaServiceCollectionExtensions.ConfigureOllamaHttpClient(...)` | **MINOR** (additive) |
| New public API: `OllamaFormat.TryFromSchema(...)` overloads | **MINOR** (additive) |
| `IOllamaGenerationClient` stream-method XML docs tightened | PATCH-like (doc only) |
| `OllamaMessage.ToolName` XML docs tightened | PATCH-like (doc only) |
| `OllamaHttpClient` ctor now takes `IOptionsMonitor<OllamaClientOptions>` | **internal** — `OllamaHttpClient` is `internal`, not part of the public API |
| DNS-class `SocketException` now maps to `OllamaConfigurationException` | Behavioural; the new exception derives from the existing `OllamaException`, so `catch (OllamaException)` handlers keep working. Documented in the CHANGELOG. |

Net result → **minor bump: `2.0.0` → `2.1.0`**.

`dotnet pack` is already pinned at `PackageValidationBaselineVersion=2.0.0` so
package-validation will prove the public surface is strictly additive.

---

## Post-merge checklist

Do these in order, from a clean local clone of `main` (or the release branch):

### 1 · Promote the `Unreleased` CHANGELOG section

Edit `CHANGELOG.md` — rename `## [Unreleased]` to the new version, and re-add
an empty `## [Unreleased]` heading above it:

```markdown
## [Unreleased]

## [2.1.0] — YYYY-MM-DD

### Added
…contents previously under [Unreleased]…
```

At the bottom of the file, keep the compare links in sync (if the file maintains
them):

```markdown
[Unreleased]: https://github.com/chethandvg/Ollama.Net/compare/v2.1.0...HEAD
[2.1.0]:      https://github.com/chethandvg/Ollama.Net/releases/tag/v2.1.0
[2.0.0]:      https://github.com/chethandvg/Ollama.Net/releases/tag/v2.0.0
```

Commit:

```bash
git checkout main && git pull
git commit -am "Prepare 2.1.0 release"
git push
```

### 2 · Local dry-run build

```bash
dotnet restore
dotnet build -c Release -warnaserror
dotnet test  -c Release --no-build
dotnet pack  src/Ollama.Net/Ollama.Net.csproj -c Release --no-build -o ./artefacts
```

Package validation (already wired into `dotnet pack`) will fail if you
accidentally removed or renamed a public API. Fix any reported breaks before
tagging — **do not** suppress them.

Confirm the packed version:

```bash
ls ./artefacts/
# → OllamaNet.Client.2.1.0.nupkg
# → OllamaNet.Client.2.1.0.snupkg
```

> MinVer derives the version from `git describe` — so if `HEAD` is one commit
> past `v2.0.0` you'll see `2.1.0-preview.0.1` until you actually push the tag.
> That's expected for local runs.

### 3 · Tag and push

From `main` at the release commit:

```bash
# Signed tag is preferred; drop '-s' if you don't have GPG set up.
git tag -s v2.1.0 -m "Ollama.Net 2.1.0"
git push origin v2.1.0
```

That kicks off `.github/workflows/release.yml`, which:

1. builds for `net8.0` / `net9.0` / `net10.0`,
2. runs the full test suite,
3. `dotnet pack`s with package-validation against the `2.0.0` baseline,
4. pushes `.nupkg` + `.snupkg` to `https://api.nuget.org/v3/index.json`,
5. creates the matching GitHub release with auto-generated notes and the
   `.nupkg` attached as an asset.

### 4 · Publishing secrets / environment

Nothing to change for this release — the `nuget` deployment environment and
`NUGET_API_KEY` secret from the `2.0.0` release are reused as-is. If the API
key has rotated, update it in
`Settings → Environments → nuget → secrets` before tagging.

### 5 · Post-publish smoke-test

Once NuGet.org shows the package at
`https://www.nuget.org/packages/OllamaNet.Client/2.1.0`:

```bash
mkdir /tmp/smoke-2.1 && cd /tmp/smoke-2.1
dotnet new console
dotnet add package OllamaNet.Client --version 2.1.0
```

Paste the following into `Program.cs` — it exercises two of the new APIs end
to end (DI seam + `DisallowPrivateNetworks`):

```csharp
using Microsoft.Extensions.DependencyInjection;
using Ollama.Net.Abstractions;
using Ollama.Net.DependencyInjection;

ServiceCollection services = new();
services.AddLogging();
services.AddOllamaClient(o =>
{
    o.BaseAddress = new Uri("http://localhost:11434/");
    o.DisallowPrivateNetworks = false; // or true for public deployments
});

// ConfigureOllamaHttpClient returns the named IHttpClientBuilder
services.ConfigureOllamaHttpClient();

ServiceProvider sp = services.BuildServiceProvider();
IOllamaClient client = sp.GetRequiredService<IOllamaClient>();
Console.WriteLine($"BaseAddress: {client.Options.BaseAddress}");
```

`dotnet run` — must compile and print the expected base address.

### 6 · Bump package-validation baseline *after* publish

After `2.1.0` is live on NuGet:

1. Edit `src/Ollama.Net/Ollama.Net.csproj` — bump
   `<PackageValidationBaselineVersion>` from `2.0.0` to `2.1.0`:

   ```xml
   <PackageValidationBaselineVersion>2.1.0</PackageValidationBaselineVersion>
   ```

2. Commit and push:

   ```bash
   git commit -am "chore: bump PackageValidationBaseline to 2.1.0"
   git push
   ```

From this point on, any PR that accidentally removes or breaks a `2.1.0`
public API will fail in CI.

### 7 · Announce (optional)

- GitHub release description: the auto-generated notes from the `v2.1.0`
  release already list commits; paste the CHANGELOG's `[2.1.0]` section at the
  top for a consumer-friendly summary.
- If you track downstream consumers, ping them that `DisallowPrivateNetworks`
  and `ConfigureOllamaHttpClient(...)` are available — these are the two
  SSRF-adjacent APIs likely to move the needle for them.

---

## Rollback

If NuGet pushes `2.1.0` but you later discover a regression:

- **Never** retag or force-push `v2.1.0`. NuGet versions are immutable.
- Ship a `2.1.1` with the fix — revert the bad commit(s), add an entry under
  `## [Unreleased] → ### Fixed`, follow the same steps above with tag
  `v2.1.1`.
- If the bug is serious enough to mislead consumers, mark `2.1.0` as
  *deprecated* on NuGet.org (package page → *Deprecate* button) with a
  one-line reason pointing at `2.1.1`.
