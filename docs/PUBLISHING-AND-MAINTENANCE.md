# 📘 Publishing & Maintenance Playbook — Ollama.Net

> A single, end-to-end, **beginner-friendly** walkthrough that takes you from
> "I have the source on my laptop" to "the package is live on NuGet.org" and
> then keeps you honest for every release after that.
>
> Read this top to bottom **once**. After that you'll only revisit the
> relevant section (usually [§3 Publishing](#3--publishing-the-first-version)
> or [§5 Maintenance](#5--long-term-maintenance)).
>
> Related docs:
> - [`VERSIONING.md`](VERSIONING.md) — deep-dive on SemVer + MinVer.
> - [`PRE-PUBLISH-CHECKLIST.md`](PRE-PUBLISH-CHECKLIST.md) — the one-off
>   checklist of GitHub/NuGet settings to tick.

---

## Contents

1. [Prerequisites](#1--prerequisites)
2. [Rehearse: publish a test release end-to-end](#2--rehearse-publish-a-test-release-end-to-end)
3. [Publishing the first version (`v1.0.0`)](#3--publishing-the-first-version-v100)
4. [After the first release — hardening](#4--after-the-first-release--hardening)
5. [Long-term maintenance](#5--long-term-maintenance)
6. [Recipes for common changes](#6--recipes-for-common-changes)
7. [Troubleshooting](#7--troubleshooting)

---

## 1 · Prerequisites

Do each step once. Tick the box after you've done it so future-you knows.

### 1.1 Tools on your machine

- [ ] [.NET 10 SDK](https://dotnet.microsoft.com/download) (it also builds for
      `net8.0` and `net9.0`; the SDK is forward-compatible).
- [ ] [`git`](https://git-scm.com/downloads).
- [ ] (Recommended) [GitHub CLI `gh`](https://cli.github.com/) — makes
      tagging/releasing a one-liner.
- [ ] (Recommended) [NuGet Package Explorer](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer)
      to eyeball your `.nupkg` before you publish it.

Verify:

```bash
dotnet --list-sdks   # expect 8.x, 9.x, 10.x
git --version
gh --version         # optional
```

### 1.2 Accounts

- [ ] **NuGet.org account** — <https://www.nuget.org/users/account/LogOn>.
      Sign in with a Microsoft/GitHub account; verify your email.
- [ ] **NuGet API key** — <https://www.nuget.org/account/apikeys>.
  - **Key name:** `Ollama.Net CI` (anything memorable).
  - **Scope:** tick *Push new packages and package versions*.
  - **Glob pattern:** `Ollama.Net*` (so the key can never push anything that
    isn't yours).
  - **Expires in:** 365 days. Put a reminder in your calendar — **an expired
    key breaks CI silently**.
  - **Copy the key now** (NuGet shows it only once).
- [ ] (Later, after 1.0.0) migrate to [trusted publishing via OIDC](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing-for-nuget-org)
      so you can delete the long-lived key entirely.

### 1.3 GitHub repo settings

Run through [`PRE-PUBLISH-CHECKLIST.md §2`](PRE-PUBLISH-CHECKLIST.md#2--repository-settings)
first. The three **must-dos** are:

- [ ] Add a repo secret called **`NUGET_API_KEY`** containing the key from
      §1.2. *Settings → Secrets and variables → Actions → New repository
      secret.*
- [ ] Create a repo environment called **`nuget`**. *Settings → Environments
      → New environment → `nuget`*. In the environment settings, add yourself
      under *Required reviewers*. This is your "safety belt": every publish
      will wait for you to click **Approve** in the Actions tab.
- [ ] *Settings → Actions → General → Workflow permissions* → **Read and write permissions** (so the `release` workflow can create a GitHub release).

### 1.4 Clone and sanity-build

```bash
git clone https://github.com/chethandvg/Ollama.Net.git
cd Ollama.Net
dotnet restore
dotnet build -c Release -warnaserror
dotnet test  -c Release --no-build
```

All three must end with **0 warnings, 0 errors, all tests green**.
If anything fails, fix it before going further — publishing bad builds is
much more painful than fixing them now.

---

## 2 · Rehearse: publish a test release end-to-end

**Never publish `1.0.0` as your very first NuGet push.** Always do a
rehearsal so you find any surprise before real consumers see it.

The rehearsal publishes a **prerelease** like `1.0.0-rc.1`. Prereleases are
hidden from NuGet search by default, so they make perfect dress rehearsals.

### 2.1 Pick your rehearsal version

Rule of thumb: use the same MAJOR.MINOR.PATCH as your eventual stable
version, plus a `-rc.1` suffix.

- Planning `v1.0.0`?  Rehearse with `v1.0.0-rc.1`.
- Planning `v1.3.0`?  Rehearse with `v1.3.0-rc.1`.

### 2.2 Check the rehearsal version locally

```bash
# Make sure your tags are up-to-date
git fetch --tags

# Dry-run pack; MinVer will compute the version from the nearest tag
dotnet pack src/Ollama.Net/Ollama.Net.csproj -c Release -o /tmp/artefacts
ls /tmp/artefacts
# Expect:
#   OllamaNet.Client.<version>.nupkg
#   OllamaNet.Client.<version>.snupkg
```

Open the `.nupkg` in NuGet Package Explorer and verify:

- [ ] **Metadata:** `PackageId = OllamaNet.Client`, `Authors = Chethan`,
      `Description` is readable, `ProjectUrl` and `RepositoryUrl` point at
      GitHub.
- [ ] **Dependencies:** one group per target framework
      (`net8.0`, `net9.0`, `net10.0`).
- [ ] **Contents:**
  - `README.md`, `CHANGELOG.md` at the package root.
  - `lib/net8.0/Ollama.Net.dll` + matching `.xml` doc file.
  - `lib/net9.0/Ollama.Net.dll` + `.xml`.
  - `lib/net10.0/Ollama.Net.dll` + `.xml`.
- [ ] **SourceLink:** right-click the `.dll` in Package Explorer → *View
      SourceLink…* → you see a `github.com/chethandvg/Ollama.Net` URL for
      each `.cs` file.

If anything is off, fix it **before** you tag. Nothing below changes what's
in the package — tagging only changes the version number stamped on it.

### 2.3 Tag the rehearsal and push

```bash
# annotated tag with a friendly message
git tag -a v1.0.0-rc.1 -m "1.0.0 release candidate 1"

# push the tag (this is what actually triggers the release workflow)
git push origin v1.0.0-rc.1
```

### 2.4 Watch the CI + release workflows

1. Open <https://github.com/chethandvg/Ollama.Net/actions>.
2. The `ci` workflow runs first (on tag push). Wait for ✅.
3. The `release` workflow runs second. It will pause at the **Approve**
   gate because the `nuget` environment requires a reviewer.
4. Click into the pending run → *Review deployments* → tick **`nuget`** →
   **Approve and deploy**.
5. Wait for ✅ on the `publish` job.

### 2.5 Verify it landed on NuGet.org

Give it ~5 min to index, then:

- Browse to `https://www.nuget.org/packages/OllamaNet.Client/1.0.0-rc.1`. The
  README and metadata should all look right.
- Check symbols: `https://www.nuget.org/packages/OllamaNet.Client/1.0.0-rc.1#readme-body-tab` → the *Debug symbols* column should say `Yes`.

### 2.6 Smoke-test as a consumer

In a **fresh** folder (not this repo):

```bash
dotnet new console -o probe
cd probe
dotnet add package OllamaNet.Client --version 1.0.0-rc.1 --prerelease
```

Paste the QuickStart snippet from the root `README.md` into `Program.cs`,
make sure you have Ollama running locally (`ollama serve` + `ollama pull
llama3.2`), then `dotnet run`.

If the call succeeds, **your package works**. 🎉

### 2.7 If something went wrong

- **Workflow failed?** Click into the failed step, read the log, fix in
  code, push, tag the next RC (`v1.0.0-rc.2`). Do **not** retag `rc.1`.
- **Metadata typo on NuGet.org?** Fix the csproj and ship `rc.2`.
  Versions cannot be edited once published.
- **Package shows the wrong version number?** Your CI checkout is shallow —
  confirm `fetch-depth: 0` in `ci.yml` and `release.yml`.

---

## 3 · Publishing the first version (`v1.0.0`)

Once the rehearsal in §2 worked end-to-end, the stable release is
**exactly the same commands with a different tag**.

### 3.1 Pre-flight

- [ ] Rehearsal `v1.0.0-rc.1` is live on NuGet and passed the consumer
      smoke-test.
- [ ] `main` is green on CI.
- [ ] Open `CHANGELOG.md` and move everything under `## [Unreleased]` into
      a new section:

  ```markdown
  ## [1.0.0] — YYYY-MM-DD

  _Copy the entries that were under `[Unreleased]` here._

  [Unreleased]: https://github.com/chethandvg/Ollama.Net/compare/v1.0.0...HEAD
  [1.0.0]:      https://github.com/chethandvg/Ollama.Net/releases/tag/v1.0.0
  ```

  Commit:

  ```bash
  git commit -am "Prepare 1.0.0 release"
  git push
  ```

### 3.2 Tag and push

```bash
git tag -a v1.0.0 -m "Ollama.Net 1.0.0"
git push origin v1.0.0
```

### 3.3 Approve the publish

Same as §2.4: Actions tab → `release` run → *Review deployments* → approve
`nuget`.

### 3.4 Verify

- [ ] `https://www.nuget.org/packages/OllamaNet.Client/1.0.0` is live.
- [ ] `dotnet add package OllamaNet.Client` (no version) picks up `1.0.0`.
- [ ] A GitHub release titled `v1.0.0` exists with auto-generated notes.

### 3.5 Announce

Celebrate however you celebrate. Post a link on your blog / socials. Open
an issue titled "🎉 v1.0.0 is out" and pin it — it's a great landing spot
for early feedback.

---

## 4 · After the first release — hardening

Do these **once**, right after `v1.0.0` lands. They add safety rails that
prevent silly mistakes in future releases.

### 4.1 Turn on package validation

Edit `src/Ollama.Net/Ollama.Net.csproj`:

```xml
<EnablePackageValidation>true</EnablePackageValidation>
<PackageValidationBaselineVersion>1.0.0</PackageValidationBaselineVersion>
```

From now on, `dotnet pack` compares the new assembly against `1.0.0` on
nuget.org and **fails the build** if you've accidentally broken the public
API without a major-version bump. This is *the* single best safeguard.

Remember to bump the baseline after every major release (e.g. `2.0.0` →
`PackageValidationBaselineVersion = 2.0.0`).

### 4.2 Enable NuGet trusted publishing (OIDC)

Long-lived API keys are the #1 cause of supply-chain incidents in NuGet.
Replace yours with OIDC as soon as NuGet allows it for your account:

1. On nuget.org → *Account* → *Trusted Publishers* → *Add new*.
2. Pick GitHub Actions, enter `chethandvg/Ollama.Net`, workflow
   `release.yml`, environment `nuget`.
3. In `.github/workflows/release.yml`, replace the *Push to NuGet.org* step
   with:

   ```yaml
   - name: Login to NuGet.org (OIDC)
     uses: NuGet/login@v1
     with:
       user: chethandvg
   - name: Push
     run: |
       dotnet nuget push "artefacts/*.nupkg"  -s https://api.nuget.org/v3/index.json --skip-duplicate
       dotnet nuget push "artefacts/*.snupkg" -s https://api.nuget.org/v3/index.json --skip-duplicate
   ```

4. Delete the `NUGET_API_KEY` secret.

### 4.3 Turn on branch protection for `main`

*Settings → Branches → Add protection rule* on `main`:

- [ ] Require a PR before merging.
- [ ] Require the `ci` workflow to pass.
- [ ] Require linear history.
- [ ] (Optional) Require signed commits.

### 4.4 Add a real package icon

Drop a 128×128 `icon.png` in `src/Ollama.Net/`, add:

```xml
<PackageIcon>icon.png</PackageIcon>
```

```xml
<None Include="icon.png" Pack="true" PackagePath="\" />
```

Then ship `1.0.1`. NuGet.org shows packages with icons far more
prominently in search.

---

## 5 · Long-term maintenance

The rhythm below keeps the library boring (in the best sense) for years.

### 5.1 Weekly

- [ ] **Merge Dependabot PRs**. They run CI automatically; if it's green,
      merge. If an update breaks tests, investigate before rubber-stamping.

### 5.2 Per issue / PR

For every bug report or feature request:

1. **Label it** (`bug`, `enhancement`, `docs`, `breaking`).
2. For bugs, write a **failing test first** in `tests/Ollama.Net.Tests/`.
3. Fix the bug in `src/Ollama.Net/`.
4. Add an entry under `## [Unreleased]` in `CHANGELOG.md`.
5. Open a PR; CI must pass with 0 warnings.

### 5.3 Per release

The release cadence is entirely up to you. Common rhythms:

- **Aggressive:** tag after every merged PR. Great for early adopters.
- **Batched:** collect a week or two of fixes and tag a minor or patch.
  Lower churn for consumers.

Deciding the bump — see the cheat sheet in
[`VERSIONING.md §3`](VERSIONING.md#3--semantic-versioning-cheat-sheet):

| You did… | Bump |
|---|---|
| Fixed a bug, no API change | `PATCH` (1.0.0 → 1.0.1) |
| Added new public API, nothing removed/renamed | `MINOR` (1.0.0 → 1.1.0) |
| Removed/renamed/retyped public API, or changed an observable behaviour | `MAJOR` (1.x → 2.0.0) |
| Not ready for everyone yet | add `-alpha.N`, `-beta.N`, `-rc.N` |

Release steps (every time):

```bash
# 1 · up-to-date main
git checkout main && git pull --ff-only

# 2 · move changelog entries to the new version section
$EDITOR CHANGELOG.md
git commit -am "Prepare <version>"
git push

# 3 · tag & push
git tag -a v<version> -m "Ollama.Net <version>"
git push origin v<version>

# 4 · approve the release in the Actions tab
```

### 5.4 Per quarter

- [ ] **Review your open issues** — close stale ones, label new ones.
- [ ] **Rotate your NuGet API key** (if you didn't move to OIDC yet).
- [ ] **Run `dotnet outdated`** (or let Dependabot do it) and bump runtime
      targets where appropriate.

### 5.5 Per .NET LTS release

- [ ] Add the new LTS TFM to `<TargetFrameworks>` in
      `src/Ollama.Net/Ollama.Net.csproj`.
- [ ] Keep **at most three** target frameworks. When you add `net12.0`,
      drop the oldest that Microsoft has EOL'd.
- [ ] Ship as `MINOR` (it's additive) or `MAJOR` if you dropped a TFM that
      consumers were using.

### 5.6 If you find a security issue yourself

- Do **not** discuss it in a public issue.
- Open a private advisory:
  `https://github.com/chethandvg/Ollama.Net/security/advisories/new`.
- Prepare the fix on a private branch (GitHub supports security forks).
- Release it as a patch of every supported version.
- Publish the advisory so nuget.org marks the old versions as vulnerable.

---

## 6 · Recipes for common changes

### 6.1 Ollama adds a new endpoint

1. Add a DTO in `src/Ollama.Net/Models/`.
2. Register it in `Internal/Json/OllamaJsonContext.cs` (AOT-friendly JSON).
3. Add a method to the relevant `I*Client` and implementation in
   `src/Ollama.Net/Clients/`.
4. Add unit tests + a WireMock end-to-end test.
5. `## [Unreleased]` → `### Added` in `CHANGELOG.md`.
6. Release as `MINOR` (additive).

### 6.2 Ollama changes an existing endpoint's JSON shape

- **Backwards compatible** (renamed field with alias, new optional field)
  → `MINOR`.
- **Breaking** (required field added, field removed, enum value meaning
  changed) → `MAJOR`.

### 6.3 You need to deprecate an API

1. Add `[Obsolete("Use Foo instead. Will be removed in 2.0.")]` on the
   symbol.
2. Keep the old behaviour working.
3. `## [Unreleased]` → `### Deprecated` in `CHANGELOG.md`.
4. Release as `MINOR`.
5. Remove the API in the next `MAJOR`. Mention it in the `### Removed`
   section.

### 6.4 You need to bump the minimum target framework

This is a **breaking change** — schedule it for a major release.

---

## 7 · Troubleshooting

| Symptom | Likely cause & fix |
|---|---|
| `dotnet pack` gives me `0.0.0-preview.0.1` | MinVer sees no tag. Run `git fetch --tags` (or in CI, ensure `fetch-depth: 0`). |
| Release workflow fails at "Push to NuGet.org" with `403` | `NUGET_API_KEY` secret is missing, expired, or scoped to a different glob. |
| Release workflow fails at pack with `NU5048 PackageReadmeFile` | The `README.md` is missing from the csproj `<None Include="README.md" Pack="true" />`. |
| I tagged the wrong commit, it hasn't published yet | `git tag -d vX.Y.Z && git push origin :refs/tags/vX.Y.Z`, then retag on the right sha and push. |
| I tagged + published the wrong version | You cannot delete it. Ship the next patch with the fix instead. |
| NuGet says the package name is taken | Pick a different `PackageId` and update every reference. `Ollama.Net` is already reserved by this repo — if you're forking, rename. |
| CI times out on Windows/macOS while downloading `WireMock.Net` | Transient — re-run the job. WireMock-OpenTelemetry pulls a lot of assets. |
| `EnablePackageValidation` fails with `PKV0001` | You added a breaking change without bumping MAJOR. Either revert the break or bump to `2.0.0` and update `PackageValidationBaselineVersion`. |

---

### 🎯 TL;DR one-liner workflow

```bash
# After every meaningful batch of changes on a clean main
git tag -a vX.Y.Z -m "Ollama.Net X.Y.Z" && git push origin vX.Y.Z
# Then approve the release in the Actions tab. That's the whole job.
```

Happy shipping! 🚀
