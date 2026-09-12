# SonarCloud analysis and proposed backlog

Snapshot: 2026-08-14, project `project-studying-dotnet_Streetcode-Server-August-2026`, unresolved issues on the default branch at commit `b169c79bd9c145d025297fa13ac3001eba066263`.

SonarCloud project: <https://sonarcloud.io/project/issues?id=project-studying-dotnet_Streetcode-Server-August-2026>

## Executive summary

SonarCloud reports **1,656 unresolved issues**: 18 Critical, 1,440 Major, 119 Minor, and 79 Info. By type, there are 8 Vulnerabilities, 10 Bugs, and 1,638 Code Smells. No open Security Hotspots were returned by the public API.

The raw number is dominated by analyzer/style findings: 1,092 StyleCop issues, 243 nullable-reference compiler warnings, 79 .NET performance suggestions, and 4 other external analyzer findings. A backlog item below represents one rule or one tightly related root cause, not one Sonar row.

| Layer/project | Issues | Share |
|---|---:|---:|
| `Streetcode.XUnitTest` | 1,017 | 61.4% |
| `Streetcode.BLL` | 432 | 26.1% |
| `Streetcode.WebApi` | 133 | 8.0% |
| `Streetcode.DAL` | 59 | 3.6% |
| Build, CI, Docker, DbUpdate | 15 | 0.9% |

## Triage decisions

### Confirmed and should enter the board first

- Security configuration: unrestricted CORS, a committed local seed password, secrets interpolated into GitHub Actions shell commands, insecure HTTP/download handling, and a root container.
- Correctness: a string EF default for a Boolean property, an always-false condition, and eight multi-collection EF queries that can cause Cartesian explosion.
- Maintainability with runtime impact: three overly complex methods, nullable-flow warnings in production code, incorrect flag-enum values, and deprecated cryptography/serialization APIs.

### Bulk cleanup, after the high-risk work

- 1,092 StyleCop findings, predominantly in 23–25 test files (`SA1101`, `SA1200`, `SA1309`, spacing/order/header rules). Fix by agreeing on `.editorconfig` first and applying one mechanical cleanup per rule family.
- 63 naming findings (`S101`), mainly the established `DTO` naming convention. Decide the convention before renaming public types because this can affect serialization, mappings, and clients.
- 79 .NET performance suggestions (`CA18xx`). Handle only after correctness and security; benchmark or test behavior-sensitive replacements.

### Do not create fix cards without confirmation

- `plsql:S1192` (2 issues) flags repeated literals in `Persistence/ScriptsMigration/DDL.sql`. Constants are not a useful remediation for generated/schema DDL. Prefer excluding generated migration scripts from code-smell analysis, with the exclusion documented.
- `S2346` correctly detects a broken `[Flags]` enum, but Sonar's literal suggestion to rename `MainAdministrator` to `None` is insufficient. The safe fix is explicit values (`None = 0`, then powers of two) plus a data/API compatibility review.
- The seed credential is only used by local seeding, but it is still a real known credential in source. Replace it with configuration or clearly disable local seeding outside `Local`; do not silently mark it false-positive.

## Proposed board cards

Cards are ordered Security/Correctness first, then Major maintainability, then Minor/Info. Estimates are team-sized ranges, not Sonar's optimistic per-line remediation time.

### P0 — Security and correctness

#### SONAR-01 — Restrict CORS to configured trusted origins

- **Rules/count:** [`S5122`](https://rules.sonarsource.com/csharp/RSPEC-5122/) — 1 vulnerability.
- **Scope:** `Streetcode.WebApi/Extensions/ServiceCollectionExtensions.cs`.
- **Cause:** `AllowAnyOrigin()` is used even though a `CORS` configuration object is loaded.
- **Done when:** allowed origins come from validated configuration; headers/methods are limited as required; local and production configuration are covered by tests; Sonar reports 0 `S5122` issues.
- **Estimate:** 0.5–1 day.

#### SONAR-02 — Remove the hard-coded local administrator password

- **Rules/count:** [`S2068`](https://rules.sonarsource.com/csharp/RSPEC-2068/) — 1 vulnerability.
- **Scope:** `Streetcode.WebApi/Extensions/SeedingLocalExtension.cs`.
- **Cause:** the `admin/admin` seed credential is committed in source.
- **Done when:** seed credentials are injected from local secrets/environment, absent outside the Local environment, and never logged; a test verifies the guard; Sonar reports 0 `S2068` issues.
- **Estimate:** 0.5 day.

#### SONAR-03 — Stop interpolating secrets in GitHub Actions run blocks

- **Rules/count:** [`S7636`](https://rules.sonarsource.com/githubactions/RSPEC-7636/) — 3 vulnerabilities.
- **Scope:** `.github/workflows/build.yml`.
- **Cause:** `secrets.SONAR_TOKEN` is expanded directly inside shell commands.
- **Done when:** the token is supplied through step `env` and referenced as a shell environment variable; no command prints it; the workflow completes; Sonar reports 0 `S7636` issues.
- **Estimate:** 1–2 hours.

#### SONAR-04 — Enforce HTTPS for downloads and parsed links

- **Rules/count:** [`S6506`](https://rules.sonarsource.com/shell/RSPEC-6506/) — 1 vulnerability; [`S5332`](https://rules.sonarsource.com/csharp/RSPEC-5332/) — 1 vulnerability.
- **Scope:** `Streetcode/build.sh`, `Streetcode.WebApi/Utils/WebParsingUtils.cs`.
- **Cause:** redirects/downloads and one URL path permit insecure HTTP.
- **Done when:** HTTPS is required, unsafe redirects are rejected, compatibility behavior is tested, and both rule counts are 0.
- **Estimate:** 0.5 day.

#### SONAR-05 — Run the API container as a non-root user

- **Rules/count:** [`docker:S6471`](https://rules.sonarsource.com/docker/RSPEC-6471/) — 1 vulnerability; `docker:S6570` — 1 Major hardening issue.
- **Scope:** `Dockerfile`.
- **Cause:** the final runtime stage has no non-root `USER` and uses a mutable base tag.
- **Done when:** the runtime user is unprivileged, required paths remain readable/writable, the health/startup flow works, the base image is pinned according to team policy, and both findings are closed.
- **Estimate:** 0.5–1 day.

#### SONAR-06 — Correct the EF Core Boolean default

- **Rules/count:** [`S9118`](https://rules.sonarsource.com/csharp/RSPEC-9118/) — 1 bug.
- **Scope:** `Streetcode.DAL/Persistence/StreetcodeDbContext.cs`.
- **Cause:** `IsKeyPartner` is Boolean but its default is the string `"false"`.
- **Done when:** the model uses `false` (Boolean), the generated migration/default constraint is correct for SQL Server, migration tests pass, and `S9118` is 0.
- **Estimate:** 0.5 day.

#### SONAR-07 — Remove the unreachable news-handler branch

- **Rules/count:** [`S2583`](https://rules.sonarsource.com/csharp/RSPEC-2583/) — 1 bug.
- **Scope:** `Streetcode.BLL/MediatR/Newss/GetNewsAndLinksByUrl/GetNewsAndLinksByUrlHandler.cs`.
- **Cause:** a newly constructed `NewsDTOWithURLs` is checked for null, so the failure branch is unreachable.
- **Done when:** validation targets the actual missing/invalid input or result, success/empty/not-found paths have tests, and `S2583` is 0.
- **Estimate:** 0.5 day.

#### SONAR-08 — Prevent EF Core Cartesian explosions

- **Rules/count:** [`S8733`](https://rules.sonarsource.com/csharp/RSPEC-8733/) — 8 bugs in 8 files.
- **Scope:** Partner, RelatedFigure, Streetcode catalog, Team handlers, and `SoftDeletingUtils`.
- **Cause:** queries eagerly include two sibling collections in a single SQL query.
- **Done when:** each query deliberately uses split queries or a bounded projection; query count/result equivalence is tested; performance is checked on representative data; `S8733` is 0.
- **Estimate:** 1–2 days.

#### SONAR-09 — Repair `UserRole` flag semantics safely

- **Rules/count:** [`S2346`](https://rules.sonarsource.com/csharp/RSPEC-2346/) — 1 Critical issue.
- **Scope:** `Streetcode.DAL/Enums/UserRole.cs` plus persistence/API consumers.
- **Cause:** `[Flags]` is applied to implicit values `0, 1, 2`; `MainAdministrator` unintentionally means zero and combinations are unsafe.
- **Done when:** intended semantics are agreed; explicit compatible values are introduced (or `[Flags]` is removed); stored values and API contracts are migrated/tested; `S2346` is 0.
- **Estimate:** 1–2 days.

### P1 — Major production-code health

#### SONAR-10 — Reduce cognitive complexity in three hotspots

- **Rules/count:** [`S3776`](https://rules.sonarsource.com/csharp/RSPEC-3776/) — 3 Critical issues.
- **Scope:** `GetStreetcodeByFilterHandler` (37), `SeedingLocalExtension` (128), `WebParsingUtils` (27); threshold 15.
- **Cause:** nested branching and multiple responsibilities in single methods.
- **Done when:** behavior is characterized by tests, responsibilities are extracted without changing results, each method is at or below 15, and all 3 findings are closed.
- **Estimate:** 2–4 days; split implementation by file if assigned to multiple people.

#### SONAR-11 — Resolve production nullable-reference warnings

- **Rules/count:** compiler `CS8618` (140), `CS8604` (38), `CS8602` (33), `CS8619` (17), `CS8601` (5), `CS8620` (4), `CS8600` (3), `CS8603` (2), `CS8634` (1): 243 total across BLL, DAL, WebApi, and tests.
- **Reference:** [C# nullable warnings](https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/nullable-warnings).
- **Cause:** DTO/entity initialization contracts and handler/mapping null flow do not match annotations.
- **Done when:** production warnings are fixed using accurate types, guards, or required initialization—not blanket null-forgiving operators; tests cover missing data; the listed compiler warnings are 0 in production projects. Test-only warnings may be a follow-up card.
- **Estimate:** 3–6 days; split by project/layer.

#### SONAR-12 — Remove unread fields and dead dependencies

- **Rules/count:** [`S4487`](https://rules.sonarsource.com/csharp/RSPEC-4487/) — 9 Critical issues across 8 handlers.
- **Cause:** injected logger/mapper/repository fields are assigned but never read.
- **Done when:** unused dependencies and constructor parameters are removed, or meaningful handling/logging uses them; DI activation tests pass; `S4487` is 0.
- **Estimate:** 0.5 day.

#### SONAR-13 — Align implementation parameter names with interfaces

- **Rules/count:** [`S927`](https://rules.sonarsource.com/csharp/RSPEC-927/) — 3 Critical issues.
- **Scope:** `GetAllCategoriesHandler`, `BlobService`, `LoggerService`.
- **Cause:** implementation names differ from interface names and can confuse named-argument callers.
- **Done when:** names match contracts, spelling is corrected, public/named-call compatibility is reviewed, and `S927` is 0.
- **Estimate:** 1–2 hours.

#### SONAR-14 — Use asynchronous EF operations in async handlers

- **Rules/count:** [`S6966`](https://rules.sonarsource.com/csharp/RSPEC-6966/) — 18 Major issues in 12 files.
- **Cause:** synchronous query/materialization calls are used on async request paths.
- **Done when:** EF async equivalents and cancellation tokens are used end-to-end, tests pass, and `S6966` is 0.
- **Estimate:** 1 day.

#### SONAR-15 — Replace obsolete framework APIs

- **Rules/count:** `SYSLIB0020` and `SYSLIB0023` — 2 Major issues.
- **Reference:** [.NET obsolete APIs](https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/obsoletions-overview).
- **Scope:** Instagram serialization and BlobService cryptography.
- **Done when:** supported APIs replace obsolete ones, stored/encrypted-data compatibility is explicitly tested, and both warnings are 0.
- **Estimate:** 1–2 days, driven by compatibility testing.

#### SONAR-16 — Fix logging templates and exception handling

- **Rules/count:** [`S2629`](https://rules.sonarsource.com/csharp/RSPEC-2629/) — 5; `S112` — 1; `S3966` — 1.
- **Scope:** `LoggerService`, `SoftDeletingUtils`, `EmailService`.
- **Cause:** eager string construction in logs, overly generic exceptions, and incorrect exception disposal/control flow.
- **Done when:** structured templates are used, domain-appropriate exceptions preserve context, exception flow is tested, and all 7 findings are 0.
- **Estimate:** 1 day.

### P2 — Mechanical cleanup and performance

#### SONAR-17 — Establish and apply the test-code StyleCop policy

- **Rules/count:** 1,092 issues: `SA1101` 529; `SA1200` 251; `SA1309` 89; `SA1009` 79; `SA1000` 48; `SA1633` 25; remaining ordering/layout rules 71.
- **Reference:** [StyleCop Analyzers rules](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/tree/master/documentation).
- **Cause:** recently added test files use a convention different from the active analyzer configuration.
- **Done when:** `.editorconfig` records the agreed policy (including headers, using placement, `this.` and underscore fields); format/fix is applied mechanically; tests pass; all enabled StyleCop findings are 0. Rules intentionally rejected are disabled with a documented rationale.
- **Estimate:** 1–3 days; preferably separate commits per rule family.

#### SONAR-18 — Decide and normalize DTO/type naming

- **Rules/count:** [`S101`](https://rules.sonarsource.com/csharp/RSPEC-101/) — 63 Minor issues.
- **Cause:** names such as `CoordinateDTO` conflict with Sonar's PascalCase acronym convention.
- **Done when:** the public naming convention is documented; renames preserve JSON/API/AutoMapper compatibility or the rule is configured accordingly; `S101` is 0 for the agreed scope.
- **Estimate:** 2–4 days if public renames are chosen; 0.5 day if the rule is configured.

#### SONAR-19 — Apply verified .NET performance analyzer fixes

- **Rules/count:** 79 Info issues: `CA1866` 30, `CA2249` 27, `CA1822` 11, and 11 findings across other `CA18xx` rules.
- **Reference:** [.NET code-quality rules](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/).
- **Cause:** avoidable allocations, suboptimal string searches, and members that can be static.
- **Done when:** behavior-sensitive changes have tests/benchmarks as appropriate, approved fixes are applied, and enabled CA findings are 0.
- **Estimate:** 1–2 days.

#### SONAR-20 — Remove dead/commented code and simplify control flow

- **Rules/count:** `S125` 8, `S1144` 3, `S1481` 4, `S1450` 3, plus `S1066`, `S2589`, `S3626`, `S1854`, `S1155`, `S1168`, `S2933` and related singletons: 27 issues.
- **Reference:** <https://rules.sonarsource.com/csharp/>
- **Cause:** abandoned comments, unused locals/members, redundant branches, and avoidable collection/null patterns.
- **Done when:** code is removed or simplified without suppression, affected paths have tests, and the listed findings are 0.
- **Estimate:** 1–2 days.

#### SONAR-21 — Consolidate repeated literals and parsing operations

- **Rules/count:** `S1192` 8, `S6562` 28, `CA2249` 27, `S1643` 1, `S1075` 1 — concentrated in `SeedingLocalExtension` and `WebParsingUtils`.
- **Reference:** <https://rules.sonarsource.com/csharp/>
- **Cause:** repeated date/string literals and repeated parsing/replacement work obscure intent and add allocation/locale risks.
- **Done when:** named constants/helpers are used where semantically stable, date parsing uses explicit culture/format, parser regression tests pass, and these findings are 0.
- **Estimate:** 1–2 days.

#### SONAR-22 — Clean up build-script portability and reliability findings

- **Rules/count:** `shelldre:S100`, `S7679`, `S7688`; `powershelldre:S8622`, `S8626`, `S8642` — 6 issues, excluding the HTTPS item already in SONAR-04.
- **Scope:** `Streetcode/build.sh`, `Streetcode/build.ps1`.
- **Cause:** non-portable shell checks/naming and PowerShell error/command handling.
- **Done when:** both scripts install/select the SDK predictably on supported platforms, failure paths are tested in CI, and the six findings are 0.
- **Estimate:** 1 day.

## Explicit exception candidate

#### SONAR-EX-01 — Exclude generated migration DDL from maintainability analysis

- **Rule/count:** [`plsql:S1192`](https://rules.sonarsource.com/plsql/RSPEC-1192/) — 2 Critical-labelled code smells in `Persistence/ScriptsMigration/DDL.sql`.
- **Rationale:** the repetitions describe independent schema objects; replacing them with a “constant” is not valid/useful portable DDL and editing generated migration history increases deployment risk.
- **Done when:** ownership/generation of the file is confirmed; the narrow path is excluded from code-smell analysis (not from security scanning); the decision is documented; no handwritten SQL is accidentally excluded.

## Tracking policy

1. Create SONAR-01 through SONAR-09 first; do not flood the board with all cleanup cards at once.
2. Add P1 cards as capacity opens, splitting only by independently testable layer/file set.
3. Assign owners during sprint planning, not during analysis. One engineer may take a subset, but the analysis author should not self-assign the entire backlog.
4. A card closes only after tests/build pass and the next SonarCloud analysis meets its stated zero-finding criterion. Suppression requires the rationale in code or analyzer configuration and a review.
5. Re-run the inventory after each merge because line-level counts and file ownership will change.
