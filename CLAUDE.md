# EFMaterializedPath

Public NuGet library (`EFMaterializedPath` on nuget.org): materialized-path tree storage for EF Core entities.
Other people's code depends on it, so treat every change as a change to a published API.

## Public library rules

- **Public surface** = every `public` type and member in `EFMaterializedPath/` (`ITreeRepository`, `TreeRepository`,
  `IMaterializedPathEntity`, `MaterializedEntityMapping`, `IIdentifierSerializer` + built-in serializers,
  `ServiceCollectionExtensions`), plus the database shape it produces (column names, defaults, indexes in
  `MaterializedEntityMapping`) and the path format (`|1|2|3|`) stored in users' databases.
- **Breaking changes** include: removing/renaming members, changing signatures, generic parameters or constraints,
  changing nullability of returns/parameters, changing the stored path format or mapping (forces data migrations),
  changing when `SaveChanges` is called, and raising the minimum target framework or EF Core version.
- Avoid breaking changes. If one is unavoidable: ask the maintainer first, prefer adding a new member and marking
  the old one `[Obsolete("...use X instead")]` for a release, and call it out explicitly in the changelog with
  migration instructions.
- Behaviour changes that keep signatures (query results, ordering, exceptions thrown) count too: cover them with a
  test and mention them in the changelog.
- Keep XML doc comments on public members accurate; they ship to consumers via IntelliSense.

## Changelog — update it with every user-visible change

- The changelog lives in `README.md` under `## Changelog`, newest entry first, heading format
  `### <version> (DD.MM.YYYY)`.
- Each entry: one or two sentences on what changed and who is affected, a **Breaking:** line if anything breaks,
  then a code block listing framework / dependency version changes as `Package old -> new`.
- Also update `<PackageReleaseNotes>` in `EFMaterializedPath/EFMaterializedPath.csproj`.
- If README usage samples or the Installation version table are affected, update them in the same change. Samples
  must compile against the current API.

## Versioning

- Package version follows the EF Core version the library is built against (8.0.2 ↔ EF Core 8.0.2,
  10.0.15 ↔ EF Core 10.0.15). Set both `<Version>` and `<AssemblyVersion>` in the library csproj.
- The EF Core reference is the minimum EF version consumers must have, so don't bump the patch without reason.
- One target framework at a time (currently `net10.0`); users on older frameworks stay on the older package line.

## Build & test

```bash
dotnet build      # must be 0 warnings; also produces the .nupkg (GeneratePackageOnBuild)
dotnet test                               # everything; needs Docker running
dotnet test --filter "Category!=Docker"   # InMemory + SQLite only, no Docker needed
```

- `EFMaterializedPath.Test`: unit tests on EF Core InMemory (xUnit v2).
- `EFMaterializedPath.IntegrationTest`: re-runs every unit test class on SQLite (in-process), PostgreSQL and
  SQL Server (Testcontainers), plus relational-only checks in `Checks/`. A new `TestTreeRepository_*` class needs
  a one-line subclass in each `Suites/*Suite.cs`; Postgres/SQL Server classes carry `Trait("Category", "Docker")`.

CI: `.github/workflows/dotnet.yml` on push/PR to `master`: job `build` (build + `Category!=Docker` tests) and job
`integration` (`Category=Docker` tests against containers). `.github/workflows/publish.yml` on `v*` tags publishes
to nuget.org (see Releasing).

## Dependency constraints

- FluentAssertions must stay on 7.x (7.2.2): 8.x has a paid commercial licence.
- InMemory is lenient (no SQL translation, no open-reader limits, ignores database defaults); changes to LINQ or
  query enumeration in `TreeRepository` must pass the integration tests, not just the unit tests.

## Releasing to nuget.org

Pushing a git tag `v<Version>` publishes the package; nothing else does. Agents push a release tag only when the
maintainer explicitly asks for that release in chat, and never run `dotnet nuget push` themselves.

How it works: `.github/workflows/publish.yml` runs on any pushed tag matching `v*`. It fails unless the tag equals
`<Version>` in `EFMaterializedPath/EFMaterializedPath.csproj` (tag `v10.0.15` ↔ `<Version>10.0.15</Version>`), then
builds Release, runs all tests (including Docker), packs, uploads the `.nupkg` as a run artifact, signs in with NuGet
trusted publishing (`NuGet/login`, OIDC, no stored API key) and pushes with `--skip-duplicate`.

One-time setup (done; recheck it if the login step fails):
- nuget.org → account → Trusted Publishing: a policy for repository owner `Meyhem`, repository `EFMaterializedPath`,
  workflow file `publish.yml`, environment `nuget`.
- GitHub repo → Settings → Secrets → Actions: `NUGET_USER` = the nuget.org username (not the email).
- GitHub repo → Settings → Environments: `nuget` (the job runs in it; required reviewers there add an approval step).

Release steps:
1. On `master`: bump `<Version>` and `<AssemblyVersion>` (see Versioning), add the README changelog entry, update
   `<PackageReleaseNotes>`, `dotnet build` with 0 warnings, `dotnet test` green. Commit.
2. `git push origin master` and wait for the `.NET` workflow to pass.
3. `git tag -a v<Version> -m "EFMaterializedPath <Version>"` and `git push origin v<Version>`. A GitHub release works
   too, if it is published (a draft creates no tag) and its tag has the `v` prefix; old tags like `1.0.8` predate this.
4. Watch Actions → "Publish to NuGet". After the push step succeeds, nuget.org validates and indexes the package:
   expect 5–15 minutes (sometimes up to an hour) before
   `https://api.nuget.org/v3-flatcontainer/efmaterializedpath/index.json` and the package page show the version.

If it goes wrong:
- Tag/version mismatch or failing tests: nothing was pushed. Fix on `master`, then move the tag:
  `git tag -d v<Version> && git push origin :refs/tags/v<Version>`, re-tag and push.
- NuGet login 401/403: the trusted publishing policy, environment name or `NUGET_USER` secret doesn't match.
- A version on nuget.org can never be overwritten or reused (only unlisted); a broken release needs a new version.
- `gh` may not be installed locally; the run can be checked in the browser or via
  `https://api.github.com/repos/Meyhem/EFMaterializedPath/actions/runs`.
