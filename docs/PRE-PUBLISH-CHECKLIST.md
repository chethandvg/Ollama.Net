# ✅ Pre-Publish Checklist — Ollama.Net

A beginner-friendly, one-off checklist. Work top to bottom; everything is
either a GitHub setting, a command to run, or a file to glance at.

When every box is ticked, you are safe to run:

```bash
git tag -a v1.0.0 -m "Ollama.Net 1.0.0"
git push origin v1.0.0
```

…and the `release` workflow will take it from there.

---

## 1 · Accounts & keys

- [ ] **NuGet.org account** created and email verified.
  <https://www.nuget.org/users/account/LogOn>
- [ ] **NuGet API key** created at
  <https://www.nuget.org/account/apikeys>.
  - Scope: *Push new packages and package versions*.
  - Glob pattern: `Ollama.Net*` (first publish **must** happen under this key).
  - Expiry: 365 days (put a reminder on your calendar).
- [ ] **GitHub secret** `NUGET_API_KEY` added at
  *Repo → Settings → Secrets and variables → Actions → New repository secret*.
- [ ] **GitHub environment** `nuget` created at
  *Repo → Settings → Environments → New environment → nuget*.
  - Add yourself as a required reviewer so every publish needs manual
    approval. (This saves you from accidental `v9.9.9` tags.)
- [ ] (Optional but recommended) reserve your package name **before** 1.0.0:
  push a `v0.1.0-rc.1` first and verify it appears on nuget.org.

## 2 · Repository settings

- [ ] *Settings → General* — confirm the description matches the one in
  `Ollama.Net.csproj`.
- [ ] *Settings → Code security* — enable:
  - [ ] Dependabot alerts
  - [ ] Dependabot security updates
  - [ ] Secret scanning
  - [ ] Private vulnerability reporting (used by `SECURITY.md`)
- [ ] *Settings → Actions → General → Workflow permissions* — set to
  **Read and write permissions**. Required so the `release` workflow can
  create a GitHub release.
- [ ] *Settings → Branches* — protect `main`:
  - [ ] Require PR before merging.
  - [ ] Require the `ci` workflow to pass.
  - [ ] Require linear history (recommended for a clean tag history).
- [ ] *Settings → Pages* — leave off, or point to `/docs` if you want a
  GitHub Pages site later (not needed for publishing).

## 3 · Local sanity run

```bash
dotnet restore
dotnet build -c Release -warnaserror
dotnet test  -c Release --no-build
dotnet pack  src/Ollama.Net/Ollama.Net.csproj -c Release -o /tmp/artefacts
```

- [ ] Build: **0 warnings, 0 errors**.
- [ ] Tests: all green.
- [ ] Pack produces `Ollama.Net.<version>.nupkg` **and**
  `Ollama.Net.<version>.snupkg` (the symbol package).
- [ ] Open the `.nupkg` with a tool like
  [NuGet Package Explorer](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer)
  and confirm:
  - [ ] `README.md`, `CHANGELOG.md`, `LICENSE` are in the package root.
  - [ ] `lib/net8.0/Ollama.Net.dll`, `lib/net9.0/Ollama.Net.dll`,
        `lib/net10.0/Ollama.Net.dll` all present, each with its `.xml` doc.
  - [ ] No stray `Krutaka.*` in any metadata field.

## 4 · Metadata review

Open `src/Ollama.Net/Ollama.Net.csproj` and double-check:

- [ ] `PackageId` = `Ollama.Net`
- [ ] `Authors` = `Chethan` (or your preferred author name — no company)
- [ ] `Description` reads well; it appears on nuget.org.
- [ ] `PackageTags` are relevant — nuget.org uses them for search.
- [ ] `RepositoryUrl` and `PackageProjectUrl` both point to
  `https://github.com/chethandvg/Ollama.Net`.
- [ ] `PackageLicenseExpression` = `MIT`.
- [ ] `PackageReadmeFile` = `README.md`.
- [ ] `IncludeSymbols`, `SymbolPackageFormat=snupkg`, `EmbedUntrackedSources`
  all still set.
- [ ] (Optional) Add a 128×128 `icon.png` next to the csproj and
  `<PackageIcon>icon.png</PackageIcon>` to brand the package.

## 5 · Documentation

- [ ] `README.md` (root) — modern, attractive, with quickstart + badges.
      Already written, but replace any `<your-name>` placeholders.
- [ ] `src/Ollama.Net/README.md` — the one consumers see on nuget.org.
      Already written.
- [ ] `CHANGELOG.md` — move everything from `[Unreleased]` into a new
      `## [1.0.0] — YYYY-MM-DD` section on the day you tag.
- [ ] `LICENSE` — kept as MIT, with your name in the copyright line (already
      present as "Chethan").
- [ ] `SECURITY.md`, `CODE_OF_CONDUCT.md`, `CONTRIBUTING.md` — already
      present; skim them once to make sure you are happy with the wording.
- [ ] `docs/VERSIONING.md` — read it **end to end**; it is your release SOP.

## 6 · Repo hygiene

- [ ] `grep -rn "Krutaka" . --exclude="OLLAMA-NUGET-PUBLISHING.md" --exclude="CHANGELOG.md" --exclude="PRE-PUBLISH-CHECKLIST.md"`
      returns **nothing**. (Those three files intentionally mention the old
      name for historical reference; investigate any other match.)
- [ ] No `bin/`, `obj/`, `packages.lock.json`, `.vs/`, or `.user` files
      committed.
- [ ] `.gitignore` — verify it already ignores the standard .NET artefacts
      (it does).

## 7 · The rehearsal (strongly recommended)

Before the real `v1.0.0`:

1. Push `v1.0.0-rc.1`:
   ```bash
   git tag -a v1.0.0-rc.1 -m "1.0.0 release candidate 1"
   git push origin v1.0.0-rc.1
   ```
2. Approve the `nuget` environment when GitHub asks.
3. Confirm the package appears at
   `https://www.nuget.org/packages/Ollama.Net/1.0.0-rc.1` within ~5 min.
4. In a fresh folder:
   ```bash
   dotnet new console -o probe && cd probe
   dotnet add package Ollama.Net --version 1.0.0-rc.1
   # paste the QuickStart sample from the README and run it.
   ```
5. Everything works? Promote to `v1.0.0`:
   ```bash
   git tag -a v1.0.0 -m "Ollama.Net 1.0.0"
   git push origin v1.0.0
   ```

## 8 · After the first release

- [ ] Turn on `<EnablePackageValidation>true</EnablePackageValidation>` and
      set `<PackageValidationBaselineVersion>1.0.0</PackageValidationBaselineVersion>`
      in `src/Ollama.Net/Ollama.Net.csproj`. CI will now fail the build if a
      non-major release accidentally breaks the public API.
- [ ] Enable [NuGet trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing-for-nuget-org)
      for your nuget.org account, replace the `NUGET_API_KEY` step in
      `.github/workflows/release.yml` with an OIDC step, then delete the
      secret. Long-lived API keys are the biggest footgun in NuGet publishing.
- [ ] Add a real `icon.png` (optional, but the package looks great with one).
- [ ] Share the package link on your blog / socials — it's live!

---

**Rule of thumb:** if a box in this checklist feels unclear, fix the unclear
thing *before* you push the tag. A NuGet version can never be edited or
deleted — only superseded. It's always cheaper to wait an hour than to ship
`1.0.1` the same afternoon.
