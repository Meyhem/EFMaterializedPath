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
`integration` (`Category=Docker` tests against containers).

## Dependency constraints

- FluentAssertions must stay on 7.x (7.2.2): 8.x has a paid commercial licence.
- InMemory is lenient (no SQL translation, no open-reader limits, ignores database defaults); changes to LINQ or
  query enumeration in `TreeRepository` must pass the integration tests, not just the unit tests.
- Publishing to nuget.org is done manually by the maintainer, never by an agent.
