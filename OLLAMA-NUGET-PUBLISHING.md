# Publishing `Krutaka.Ollama` as a Standalone NuGet Package

**Audience:** The repository owner (or anyone with admin rights to both the Krutaka repository and the target standalone repository + NuGet org).

**Goal:** Extract `src/Krutaka.Ollama`, `tests/Krutaka.Ollama.Tests`, and `samples/Krutaka.Ollama.Samples` from this monorepo into a new, standalone GitHub repository, and publish a public NuGet package (with signed, symbol‑enriched artefacts, semantic versioning, and a fully automated CI/CD pipeline).

> The library has been built with this migration in mind. No `Krutaka`‑specific types leak across the public API surface; every dependency is declared in `Directory.Packages.props` and re‑resolvable from NuGet.org; the csproj is already marked `IsPackable=true` with all package metadata populated. The steps below are mechanical.

---

## 0. Prerequisites (once)

| Item | Why | How |
|---|---|---|
| NuGet.org account and API key | Push packages | https://www.nuget.org/account/apikeys — scope: *Push new packages and package versions*, glob: `Ollama.Net.*` (once you settle on a final name) |
| GitHub account with admin rights | Create the new repo and secrets | https://github.com/new |
| .NET 10 SDK locally | Build and test | https://dotnet.microsoft.com/download |
| `git`, `gh` CLI | Scripted migration | `gh auth login` |

Decide your **public package name** now. `Krutaka.Ollama` is acceptable but tightly couples the package identity to this agent. Recommended public names, in preference order:

1. `Ollama.Net` — short, canonical, closest to Microsoft conventions (e.g. `Azure.Identity`).
2. `OllamaSharp` — already taken on NuGet; do not reuse.
3. `Dotnet.Ollama` / `Ollama.Client` — available but less idiomatic.

> **Decision affects:** namespace, `PackageId`, assembly name, repository slug, CI secrets, and every `using` in consumer code. The steps below use the placeholder `Ollama.Net` — search‑and‑replace if you pick a different name.

---

## 1. Create the new repository

```bash
gh repo create <your-org>/ollama-net \
    --public \
    --description "Modern, async, AOT-friendly .NET client for the Ollama REST API." \
    --license MIT \
    --clone
cd ollama-net
```

Initial layout:

```
ollama-net/
├── .github/workflows/        # CI/CD (created in §4)
├── src/
│   └── Ollama.Net/           # renamed from src/Krutaka.Ollama
├── tests/
│   └── Ollama.Net.Tests/
├── samples/
│   └── Ollama.Net.Samples/
├── Directory.Build.props
├── Directory.Packages.props
├── Ollama.Net.sln
├── CHANGELOG.md
├── README.md
├── LICENSE
└── .gitignore
```

---

## 2. Copy and rename the three projects

### 2.1 Copy the files

From the root of this repository:

```bash
# Assume ollama-net/ is a sibling directory.
cp -R src/Krutaka.Ollama                  ../ollama-net/src/Ollama.Net
cp -R tests/Krutaka.Ollama.Tests          ../ollama-net/tests/Ollama.Net.Tests
cp -R samples/Krutaka.Ollama.Samples      ../ollama-net/samples/Ollama.Net.Samples
cp Directory.Packages.props                ../ollama-net/
cp .editorconfig                           ../ollama-net/
```

Delete the monorepo's `bin/`, `obj/`, and `packages.lock.json` from the copies before the first commit.

### 2.2 Rename project files

```bash
cd ../ollama-net
mv src/Ollama.Net/Krutaka.Ollama.csproj          src/Ollama.Net/Ollama.Net.csproj
mv tests/Ollama.Net.Tests/Krutaka.Ollama.Tests.csproj  tests/Ollama.Net.Tests/Ollama.Net.Tests.csproj
mv samples/Ollama.Net.Samples/Krutaka.Ollama.Samples.csproj samples/Ollama.Net.Samples/Ollama.Net.Samples.csproj
```

### 2.3 Global namespace and identifier rename

```bash
# Namespaces and assembly identifiers
grep -rl "Krutaka\.Ollama" --include='*.cs'   | xargs sed -i 's/Krutaka\.Ollama/Ollama.Net/g'
grep -rl "Krutaka\.Ollama" --include='*.csproj'| xargs sed -i 's/Krutaka\.Ollama/Ollama.Net/g'
grep -rl "Krutaka\.Ollama" --include='*.md'   | xargs sed -i 's/Krutaka\.Ollama/Ollama.Net/g'
# Project-reference paths
sed -i 's|..\\..\\src\\Krutaka\.Ollama|..\\..\\src\\Ollama.Net|g' tests/Ollama.Net.Tests/Ollama.Net.Tests.csproj samples/Ollama.Net.Samples/Ollama.Net.Samples.csproj 2>/dev/null || true
sed -i 's|\.\./\.\./src/Krutaka\.Ollama|../../src/Ollama.Net|g' tests/Ollama.Net.Tests/Ollama.Net.Tests.csproj samples/Ollama.Net.Samples/Ollama.Net.Samples.csproj
```

Sanity check — **there should be zero results**:

```bash
grep -rn "Krutaka" --include='*.cs' --include='*.csproj' --include='*.md' .
```

### 2.4 Update package metadata in `src/Ollama.Net/Ollama.Net.csproj`

Set the following properties (see §6 for versioning):

```xml
<PropertyGroup>
  <!-- Multi-target for maximum reach. .NET 10 for latest consumers; 8/9 for
       long-tail and LTS users. If you need to target only net10.0, remove the
       other TargetFrameworks values. -->
  <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>

  <PackageId>Ollama.Net</PackageId>
  <Authors>your-name-or-org</Authors>
  <Company>your-org</Company>
  <Copyright>© your-name $(CurrentYear)</Copyright>
  <Description>Modern, async, AOT-friendly .NET client for the Ollama REST API.</Description>
  <PackageTags>ollama;ai;llm;client;rest;http;streaming</PackageTags>
  <PackageProjectUrl>https://github.com/&lt;your-org&gt;/ollama-net</PackageProjectUrl>
  <RepositoryUrl>https://github.com/&lt;your-org&gt;/ollama-net</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageIcon>icon.png</PackageIcon> <!-- optional; add a 128x128 PNG next to the csproj -->
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)' == 'true'">true</ContinuousIntegrationBuild>
  <Deterministic>true</Deterministic>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <IsTrimmable>true</IsTrimmable>
  <IsAotCompatible>true</IsAotCompatible>
  <EnablePackageValidation>true</EnablePackageValidation>
  <!-- Set once the first stable 1.0.0 has been published: -->
  <!-- <PackageValidationBaselineVersion>1.0.0</PackageValidationBaselineVersion> -->
</PropertyGroup>
```

The existing `Directory.Packages.props` you copied already declares every runtime NuGet dependency needed.

### 2.5 Multi‑targeting adjustments

Verify that source‑generated JSON, `GeneratedRegex`, required members, and collection expressions all compile under `net8.0`. If CI flags any `net8.0`‑only issue, conditionalise with `#if NET8_0` — but given the library was designed with AOT/trim in mind, this is unlikely to be needed beyond minor package version adjustments via `Condition="'$(TargetFramework)' == 'net8.0'"` in `Directory.Packages.props`.

### 2.6 Add a solution file

```bash
dotnet new sln --name Ollama.Net
dotnet sln Ollama.Net.sln add src/Ollama.Net/Ollama.Net.csproj
dotnet sln Ollama.Net.sln add tests/Ollama.Net.Tests/Ollama.Net.Tests.csproj
dotnet sln Ollama.Net.sln add samples/Ollama.Net.Samples/Ollama.Net.Samples.csproj
```

### 2.7 First build/test

```bash
dotnet restore
dotnet build -c Release
dotnet test  -c Release
```

All 78 tests must pass and the build must produce **zero warnings, zero errors**. If anything breaks, it is almost certainly a missed `Krutaka.Ollama` → `Ollama.Net` rename in a `using` directive.

---

## 3. Documentation artefacts

The project ships with `README.md` and `CHANGELOG.md` (both already packaged via `<None Include="…" Pack="true" />`). Before the first tag:

- [ ] **`README.md`** — update the package name, badge URLs, install snippets (`dotnet add package Ollama.Net`), and the "Why another client?" section.
- [ ] **`CHANGELOG.md`** — add a `## [1.0.0] — YYYY-MM-DD` entry describing the migration; follow [Keep a Changelog](https://keepachangelog.com/).
- [ ] **`LICENSE`** — keep MIT (pre‑created by `gh repo create`).
- [ ] **`SECURITY.md`** — one short paragraph telling users where to file vulnerabilities (GitHub private advisories work well).
- [ ] **`CODE_OF_CONDUCT.md`** — use the Contributor Covenant template.
- [ ] **`CONTRIBUTING.md`** — minimum rules: open an issue first, follow the `.editorconfig`, `dotnet test` must pass with zero warnings.
- [ ] **`.github/dependabot.yml`** — weekly updates for `nuget` and `github-actions`.

---

## 4. CI pipeline (`.github/workflows/ci.yml`)

Runs on every push and PR to `main` and any branch. Builds, tests, and packs on matrix OS + TFM, uploading the `.nupkg` as an artefact.

```yaml
name: ci
on:
  push:
    branches: [main]
    tags: ['v*']
  pull_request:
    branches: [main]

permissions:
  contents: read

jobs:
  build:
    strategy:
      fail-fast: false
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0      # needed by MinVer/Nerdbank.GitVersioning
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: |
            8.0.x
            9.0.x
            10.0.x
      - name: Restore
        run: dotnet restore
      - name: Build
        run: dotnet build -c Release --no-restore /warnaserror
      - name: Test
        run: dotnet test -c Release --no-build --verbosity normal --collect:"XPlat Code Coverage"
      - name: Pack (main and tags only)
        if: matrix.os == 'ubuntu-latest' && (github.ref == 'refs/heads/main' || startsWith(github.ref, 'refs/tags/'))
        run: dotnet pack src/Ollama.Net/Ollama.Net.csproj -c Release --no-build -o artefacts
      - uses: actions/upload-artifact@v4
        if: matrix.os == 'ubuntu-latest' && (github.ref == 'refs/heads/main' || startsWith(github.ref, 'refs/tags/'))
        with:
          name: nupkg
          path: artefacts/*.*nupkg
```

**Why these settings**

- `fetch-depth: 0` — required by any commit‑driven version generator (see §6).
- `/warnaserror` — the project compiles warning‑free; the CI must keep it that way.
- **Ubuntu only for pack** — keep artefacts deterministic; the package is platform‑agnostic.
- Pack on `main` and tags — gives you a continuously published prerelease feed from `main` if you later add a `prerelease` job.

---

## 5. Release pipeline (`.github/workflows/release.yml`)

Triggers only on a `v*` tag, reuses the CI artefact, and pushes to NuGet.org.

```yaml
name: release
on:
  push:
    tags: ['v*']

permissions:
  contents: write           # for GitHub release creation
  id-token: write           # reserved for OIDC publishing (see note below)

jobs:
  publish:
    runs-on: ubuntu-latest
    environment: nuget      # protected — requires manual approval if configured
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - name: Pack
        run: |
          dotnet restore
          dotnet build -c Release --no-restore
          dotnet test  -c Release --no-build
          dotnet pack  src/Ollama.Net/Ollama.Net.csproj -c Release --no-build -o artefacts
      - name: Push to NuGet.org
        env:
          NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
        run: |
          dotnet nuget push "artefacts/*.nupkg"  -s https://api.nuget.org/v3/index.json -k "$NUGET_API_KEY" --skip-duplicate
          dotnet nuget push "artefacts/*.snupkg" -s https://api.nuget.org/v3/index.json -k "$NUGET_API_KEY" --skip-duplicate
      - name: Create GitHub release
        uses: softprops/action-gh-release@v2
        with:
          generate_release_notes: true
          files: artefacts/*.*nupkg
```

**Required repository secrets** (*Settings → Secrets and variables → Actions*):

| Name | Value |
|---|---|
| `NUGET_API_KEY` | NuGet.org API key scoped to `Ollama.Net.*` |

**Required repository environment** (*Settings → Environments → New environment* → `nuget`):
- Add required reviewers (yourself at minimum) for a manual approval gate.

> NuGet.org now supports [trusted publishing via OIDC](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing-for-nuget-org). Once enabled for your account, replace `NUGET_API_KEY` with an OIDC step and remove the secret entirely. This is the strongest protection against leaked API keys.

---

## 6. Version management

Pick exactly one strategy. Both satisfy SemVer 2.0.0 and both interoperate with the workflows above.

### Option A — Git‑tag driven with [MinVer](https://github.com/adamralph/minver) (recommended)

Add to `Directory.Packages.props`:

```xml
<PackageVersion Include="MinVer" Version="6.0.0" />
```

Add to `src/Ollama.Net/Ollama.Net.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="MinVer" PrivateAssets="all" />
</ItemGroup>
<PropertyGroup>
  <MinVerTagPrefix>v</MinVerTagPrefix>
  <MinVerDefaultPreReleaseIdentifiers>preview.0</MinVerDefaultPreReleaseIdentifiers>
</PropertyGroup>
```

Release procedure:

```bash
# Bug fix
git tag v1.0.1 && git push origin v1.0.1

# New feature (minor)
git tag v1.1.0 && git push origin v1.1.0

# Breaking change (major)
git tag v2.0.0 && git push origin v2.0.0

# Prerelease
git tag v1.2.0-beta.1 && git push origin v1.2.0-beta.1
```

MinVer computes the package version from the nearest tag plus commit height, so builds of `main` between tags publish as `1.1.0-preview.0.N` automatically.

### Option B — Manual `Version` property

Hand‑edit `<Version>` in the csproj and tag with the same value. Simpler, but human‑error‑prone.

### SemVer discipline

- **`MAJOR`** — any breaking change to the public API, even if you "know nobody is using it yet".
- **`MINOR`** — new API surface that is fully backwards compatible.
- **`PATCH`** — internal fix, dependency bump, doc change.
- **Prereleases** (`1.2.0-alpha.1`, `-beta.2`, `-rc.1`) for anything that is not ready for general consumption.
- Once `1.0.0` ships, enable `<EnablePackageValidation>true</EnablePackageValidation>` and set `<PackageValidationBaselineVersion>` to the previous stable version — this fails the build if a breaking change is accidentally introduced in a non‑major release.

---

## 7. First release walkthrough

1. Complete sections 1–5.
2. `dotnet restore && dotnet build -c Release && dotnet test -c Release` — **0 warnings / 0 errors / all tests green**.
3. `git commit -am "Initial import from Krutaka monorepo" && git push origin main`
4. Watch the `ci` workflow go green.
5. `git tag v1.0.0 && git push origin v1.0.0`
6. Approve the `nuget` environment when the `release` workflow requests it.
7. Verify the package at `https://www.nuget.org/packages/Ollama.Net/1.0.0` (allow ~5 min for indexing).
8. Test the live package in a throwaway console app:
   ```bash
   dotnet new console -o probe && cd probe
   dotnet add package Ollama.Net --version 1.0.0
   # paste the QuickStart sample, run, confirm output
   ```

---

## 8. Ongoing maintenance checklist

| Trigger | Action |
|---|---|
| Ollama releases a new API endpoint | Add DTO in `Models/`, add method to the relevant `I*Client` and the implementation, add unit test, add a WireMock end‑to‑end test, mention in `CHANGELOG.md`. |
| Ollama **changes** an existing endpoint's JSON shape | Update the DTO with a backwards‑compatible rename or a new property; bump **MINOR** if both shapes remain valid, **MAJOR** if consumers must re‑type. |
| .NET releases a new LTS | Add it to `<TargetFrameworks>`; keep at most three. |
| Dependabot opens a PR | Merge when CI is green. |
| A consumer reports a bug | Write a failing test first (this is what uncovered the four bugs in the initial implementation — every regression belongs in the WireMock end‑to‑end suite). |

---

## 9. Integrating back into Krutaka

Once `Ollama.Net@1.0.0` is published, deleting `src/Krutaka.Ollama` from this repo and replacing it with a `PackageReference` is a one‑line change:

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="Ollama.Net" Version="1.0.0" />
```

```xml
<!-- wherever Krutaka.Console currently depends on Krutaka.Ollama -->
<PackageReference Include="Ollama.Net" />
```

…then global‑rename `using Krutaka.Ollama` → `using Ollama.Net` across the Krutaka solution. Because every public type in `Krutaka.Ollama` is a plain record/interface/enum — nothing references `Krutaka.Core` — this migration is a no‑op at runtime.

---

## Appendix A — Rename cheat sheet

If you prefer `Ollama.Client` or another name, `Ollama.Net` appears in exactly these files after §2:

- every `.cs` file's `namespace` and `using` directives
- `src/Ollama.Net/Ollama.Net.csproj` (`PackageId`, `AssemblyName`, `RootNamespace`)
- `tests/Ollama.Net.Tests/Ollama.Net.Tests.csproj` (`AssemblyName`, `InternalsVisibleTo` target in `AssemblyInfo.cs`)
- `src/Ollama.Net/AssemblyInfo.cs` — update `InternalsVisibleTo("Ollama.Net.Tests")`.
- `README.md`, `CHANGELOG.md`, `.github/workflows/*.yml`

A single `sed` over the whole tree is sufficient.

## Appendix B — Files explicitly **not** copied from the monorepo

These are Krutaka‑specific and must not land in the standalone repository:

- `AGENTS.md`, `docs/status/*.md`, `docs/roadmap/ROADMAP.md`, `docs/versions/*.md`
- `.github/instructions/*`, `.github/copilot-instructions.md`
- Everything under `src/Krutaka.Core`, `src/Krutaka.Console`, `src/Krutaka.Telegram`, `src/Krutaka.Tools`, `src/Krutaka.AI`, `src/Krutaka.Memory`, `src/Krutaka.Skills`
- Everything under `tests/` other than `tests/Krutaka.Ollama.Tests`
- Everything under `samples/` other than `samples/Krutaka.Ollama.Samples`

## Appendix C — Source signing (optional, recommended for 1.0+)

For packages distributed to enterprise users, sign the assembly and the `.nupkg`.

1. Obtain a code‑signing certificate (e.g. DigiCert, SignPath, or a free `SignPath.io` plan for OSS).
2. Store the certificate as a base64 GitHub secret named `SIGNING_CERT_BASE64` and its password as `SIGNING_CERT_PWD`.
3. Add to the `release` workflow, between *Pack* and *Push*:
   ```yaml
   - name: Sign
     run: |
       echo "$SIGNING_CERT_BASE64" | base64 -d > cert.pfx
       dotnet nuget sign "artefacts/*.nupkg" \
           --certificate-path cert.pfx \
           --certificate-password "$SIGNING_CERT_PWD" \
           --timestamper http://timestamp.digicert.com
   ```

---

**Summary:** §1–§2 migrate the code, §3 dresses the repository, §4 sets up CI, §5 sets up release, §6 fixes versioning, §7 drives the first tag. Everything after that is routine maintenance and optional hardening.
