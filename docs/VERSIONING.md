# 📦 Versioning Guide — a Beginner-Friendly Playbook (MinVer)

> **TL;DR** — You never edit a version number by hand. You just tag a commit
> in git, e.g. `git tag v1.2.3`, push the tag, and the CI does everything else.

This document walks you through versioning **Ollama.Net** with [MinVer](https://github.com/adamralph/minver)
from zero prior knowledge. Keep it bookmarked.

---

## 1 · The five-second mental model

- The **package version** (e.g. `1.2.3`) comes **entirely** from the nearest
  git tag that starts with `v`.
- Tags like `v1.2.3` create a **stable** version `1.2.3`.
- Tags like `v1.2.3-beta.1` create a **prerelease** version `1.2.3-beta.1`.
- Commits made **after** the latest tag are published as
  `<next>-preview.0.<N>`, where `N` is how many commits past the tag you are.
  Example: one commit after `v1.2.3` ⇒ `1.2.4-preview.0.1`.

That's it. Everything below is just operating that mental model safely.

---

## 2 · The one-time setup (already done in this repo)

Nothing to do as a user — this is for your reference so you know why things
"just work".

1. MinVer is referenced in `src/Ollama.Net/Ollama.Net.csproj`:
   ```xml
   <PackageReference Include="MinVer" PrivateAssets="all" />
   <PropertyGroup>
     <MinVerTagPrefix>v</MinVerTagPrefix>
     <MinVerDefaultPreReleaseIdentifiers>preview.0</MinVerDefaultPreReleaseIdentifiers>
   </PropertyGroup>
   ```
2. MinVer's version is pinned centrally in `Directory.Packages.props`.
3. Both CI workflows check out the repo with `fetch-depth: 0` so MinVer can see
   the full tag history.

---

## 3 · Semantic Versioning cheat sheet

Versions look like `MAJOR.MINOR.PATCH`. Bump the part that matches the change:

| Change you made | Bump | Example |
|---|---|---|
| Fixed a bug, no API change | `PATCH` | `1.2.3` → `1.2.4` |
| Added a new class / method / overload (no removals, no renames) | `MINOR` | `1.2.3` → `1.3.0` |
| Removed / renamed a public API, changed a method signature, changed behaviour in a way that can break a consumer | `MAJOR` | `1.2.3` → `2.0.0` |
| Not ready for everyone yet (experimenting, gathering feedback) | add a prerelease suffix | `2.0.0-alpha.1`, `-beta.2`, `-rc.1` |

**Golden rule:** if in doubt whether a change is breaking, treat it as
breaking. "Technically it compiles" is not good enough for a library — any
observable behaviour change can break someone's build in a few months.

---

## 4 · Your day-to-day flow

### 4.1 Releasing a bug-fix

```bash
# make sure you're on a clean main
git checkout main && git pull --ff-only

# optionally update CHANGELOG.md — move items from [Unreleased] to [1.2.4]
$EDITOR CHANGELOG.md
git commit -am "Prepare 1.2.4"
git push

# create the tag and push it — this is the release
git tag -a v1.2.4 -m "Ollama.Net 1.2.4"
git push origin v1.2.4
```

The `release` workflow triggers, pack runs, the `nuget` environment asks you
for approval, and `Ollama.Net 1.2.4` shows up at
<https://www.nuget.org/packages/OllamaNet.Client> within ~5 minutes.

### 4.2 Releasing a new feature (minor)

Same flow, just bump the minor part:

```bash
git tag -a v1.3.0 -m "Ollama.Net 1.3.0"
git push origin v1.3.0
```

### 4.3 Releasing a breaking change (major)

```bash
git tag -a v2.0.0 -m "Ollama.Net 2.0.0"
git push origin v2.0.0
```

### 4.4 Cutting a prerelease

Use when a feature is merged to `main` but not yet considered stable.

```bash
# first preview
git tag -a v1.3.0-beta.1 -m "1.3.0 beta 1"
git push origin v1.3.0-beta.1

# subsequent previews
git tag -a v1.3.0-beta.2 -m "1.3.0 beta 2"
git push origin v1.3.0-beta.2

# when ready for general availability
git tag -a v1.3.0 -m "Ollama.Net 1.3.0"
git push origin v1.3.0
```

Prereleases are **not** shown to consumers by default on NuGet — they need to
tick "Include prerelease" or use `--prerelease` with the CLI. That's exactly
what you want.

### 4.5 What gets built between tags?

Every commit to `main` that is built by CI gets a MinVer-computed version like
`1.3.0-preview.0.7` (7 commits past `v1.2.9`). By default, `ci.yml` only packs
on Ubuntu and does **not** push these to NuGet.org. If you ever want a public
nightly feed, add a second "push preview" job that pushes only when
`github.ref == 'refs/heads/main'`.

---

## 5 · The ordering rule you must never break

For prereleases MinVer (and SemVer) compare identifiers **segment by segment**:

```
1.3.0-alpha.1  <  1.3.0-alpha.2  <  1.3.0-beta.1  <  1.3.0-rc.1  <  1.3.0
```

Meaning: if you tagged `v1.3.0-beta.1`, you **cannot** go back and publish
`v1.3.0-alpha.3` — NuGet will happily accept it, but it will sort **before**
`beta.1`, confusing every consumer. Always move forward.

---

## 6 · Common mistakes and how to recover

| Mistake | Fix |
|---|---|
| "I forgot to pull tags, I'm getting a weird version locally." | `git fetch --tags`. MinVer reads from tags, not from branch names. |
| "I pushed `v1.2.3` but the CI built `0.0.0-preview.0.1`." | Your checkout is shallow. In CI use `actions/checkout` with `fetch-depth: 0` (already configured). |
| "I deleted a tag and re-pushed it to a different commit." | **Never do this** if the tag was already picked up by the `release` workflow — the version exists on NuGet forever. Ship a new patch instead. |
| "I tagged the wrong commit." | If it was **not** published, delete and retag: `git tag -d v1.2.3 && git push origin :refs/tags/v1.2.3 && git tag v1.2.3 <sha> && git push origin v1.2.3`. If it **was** published, bump and ship `v1.2.4` instead. |
| "I need two fixes but they're not ready at the same time." | Cherry-pick the first into `main`, tag, release; cherry-pick the second, tag, release. Small, frequent releases are better than large, rare ones. |

---

## 7 · Post-`1.0.0` hardening

Once you've published `v1.0.0`, enable package validation to stop yourself
from accidentally shipping a breaking change without a major bump. In
`src/Ollama.Net/Ollama.Net.csproj`:

```xml
<EnablePackageValidation>true</EnablePackageValidation>
<PackageValidationBaselineVersion>1.0.0</PackageValidationBaselineVersion>
```

From that point on, `dotnet pack` will compare the new assembly against the
previously published `1.0.0` and fail the build on any incompatible change.
Bump the baseline when you ship a new major.

---

## 8 · Appendix — verifying locally before tagging

```bash
# What version would MinVer give me right now?
dotnet build src/Ollama.Net -c Release --no-restore /p:DebugVersion=true \
    | grep -i "MinVer"

# Full dry-run pack
dotnet pack src/Ollama.Net -c Release -o /tmp/artefacts
ls /tmp/artefacts
# Ollama.Net.<version>.nupkg
# Ollama.Net.<version>.snupkg
```

If that version is what you expected, tag it. If not — usually `fetch --tags`
was missing — fix that first.

---

**Remember:** tag, push, approve, done. No hand-edited version numbers, ever.
