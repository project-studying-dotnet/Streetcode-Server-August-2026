<div align="center"><img src="StreetCodeLogo.jpg" title="SoftServe IT Academy" alt="SoftServe IT Academy"></div>

# Streetcode — Server (August 2026 cohort)

Back-end (ASP.NET Core Web API) of the Streetcode project, used as the working codebase of the **August 2026** .NET cohort.

> ### **Vision**
> The largest platform about the history of Ukraine, built in the space of cities.

> ### **Mission**
> To fill the gaps in the historical memory of Ukrainians.

| | |
|---|---|
| Repository | `project-studying-dotnet/Streetcode-Server-August-2026` |
| Default branch | `dev` |
| Board | [project #30 · Streetcode-August-2026](https://github.com/orgs/project-studying-dotnet/projects/30) (private) |
| Team | `net-team-august-2026` |

The codebase originates from [ita-social-projects/StreetCode](https://github.com/ita-social-projects/StreetCode). Each cohort starts from the same reference tree rather than from the previous cohort's work. This cohort works on the server only — no client repository is provisioned, and the `Streetcode/StreetCode.Client` submodule stays uninitialised.

---

## Table of Contents

- [Tech stack](#tech-stack)
- [Getting started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Clone](#clone)
  - [Database](#database)
  - [Run](#run)
  - [Tests](#tests)
  - [Code style](#code-style)
- [Workflow](#workflow)
  - [Access](#access)
  - [Branches](#branches)
  - [Board flow](#board-flow)
  - [Pull requests](#pull-requests)
  - [Hotfixes](#hotfixes)
- [Project layout](#project-layout)
- [Known gaps](#known-gaps)
- [FAQ](#faq)
- [License](#license)

---

## Tech stack

ASP.NET Core 10 · EF Core 10 (SQL Server) · MediatR · AutoMapper · FluentResults · Hangfire · Serilog · Swashbuckle · StyleCop.Analyzers · xUnit · Nuke Build

---

## Getting started

### Prerequisites

* [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) — the solution targets `net10.0`
* [Visual Studio 2026](https://visualstudio.microsoft.com/) (Version 18.0), JetBrains Rider, or VS Code. **Visual Studio 2019 & 2022 do not support `net10.0`** and cannot open this solution.
* [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) 2019+ (Express edition is enough) **or** [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Clone

```bash
git clone https://github.com/project-studying-dotnet/Streetcode-Server-August-2026.git
cd Streetcode-Server-August-2026
```

`dev` is the default branch and the base for all work.

### Database

The application reads the database connection string from the standard `ConnectionStrings:DefaultConnection` configuration key.

For local development, copy the tracked environment template:

```bash
cp .env.example .env
```

Then replace the placeholder in `.env` with the connection string for the local SQL Server instance:

```dotenv
STREETCODE_ConnectionStrings__DefaultConnection="Server=localhost;Database=StreetcodeDb;User Id=sa;Password=your_password;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

The `STREETCODE_` prefix is removed by the environment configuration provider, and the double underscore `__` represents the configuration section separator `:`. Therefore, `STREETCODE_ConnectionStrings__DefaultConnection` overrides `ConnectionStrings:DefaultConnection`.

The `.env` file is ignored by Git and must never be committed. Do not put real credentials in `appsettings*.json` or `.env.example`.

#### Option A — SQL Server in a container

Replace `your_strong_password` with a strong local password before running the command:

```bash
docker run -d --name streetcode-db \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=your_strong_password" \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

For this setup, use `127.0.0.1,1433` as the server, `StreetcodeDb` as the database, `sa` as the user, and the same password that was supplied to the container.

#### Option B — a local SQL Server instance

Set `STREETCODE_ConnectionStrings__DefaultConnection` to a complete connection string for the local SQL Server instance. For a named SQL Server instance, the `Server` value can be set to something such as `localhost\SQLEXPRESS`.

EF Core, Hangfire, and DbUpdate read the same `ConnectionStrings:DefaultConnection` configuration value.

The examples use `TrustServerCertificate=True` only for local development, where SQL Server may use a self-signed certificate. Production environments should use a properly configured and validated server certificate.

`Env.TraversePath().Load()` searches for `.env` in the current directory and its parent directories, so locating the root `.env` file no longer requires the repository root to be used as the working directory.

The schema is created on startup — `ApplyMigrations` runs `MigrateAsync()`, so an empty database is enough. Seed data is **not** loaded: the `SeedDataAsync()` call in `Program.cs` is commented out, so endpoints return empty collections until data is added.

### Run

Set **Streetcode.WebApi** as the startup project and use the **`Streetcode_Local`** launch profile, or:

```bash
dotnet run --project Streetcode/Streetcode.WebApi --launch-profile Streetcode_Local
```

| | |
|---|---|
| Swagger UI | <https://localhost:5001/swagger> |
| HTTP | <http://localhost:5000> |
| Hangfire dashboard | `/dash` |

The `Local` environment is what enables Swagger and suppresses the recurring background jobs. Under any other profile Swagger is off, HSTS is on, and Hangfire starts web-parsing jobs — use Postman or another client against `http://localhost:5000`.

Run `dotnet dev-certs https --trust` once, since the pipeline enforces HTTPS redirection.

> A missing connection string stops startup with an explicit configuration error. A non-empty but invalid or unreachable database connection is logged by `ApplyMigrations`, while the host continues to start and subsequent database requests fail. Look for `An error occured during startup migration` in the console.

### Tests

```bash
dotnet test Streetcode/Streetcode.XUnitTest        # unit tests
dotnet test Streetcode/Streetcode.XIntegrationTest # integration tests
```

Integration tests that require a database use the same `STREETCODE_ConnectionStrings__DefaultConnection` environment variable. Point it to a separate test database, such as `StreetcodeDbtest`, and never reuse production credentials. The `appsettings.IntegrationTests.json` file contains only non-sensitive environment-specific settings.

### Code style

StyleCop.Analyzers is wired through `Streetcode/settings.ruleset`. Its findings are warnings and do not fail the build; the reference tree already carries a large number of them, so keep new code clean rather than trying to zero the counter.

---

## Workflow

### Access

Membership in the `net-team-august-2026` team grants `push` on the repository and `WRITER` on the board — these are two separate access lists. **Do not fork**: branches are created directly in this repository.

### Branches

| Branch | Purpose | Approvals to merge |
|---|---|---|
| `dev` | default, integration branch, base of every PR | 2 |
| `main` | release | 1 |

Both branches are protected: no force-push, no deletion, stale approvals are dismissed on a new push, and every conversation must be resolved.

Name a working branch after the task:

```
type/SSAD-<number>/short-description
```

for example `feature/SSAD-42/add-partner-endpoint`. Start it from an up-to-date `dev`:

```bash
git switch dev && git pull
git switch -c feature/SSAD-42/add-partner-endpoint
```

### Board flow

Sprint iterations last 7 days and start on Wednesday. A task travels the board as:

```
Sprint N Backlog (draft)  →  Convert to issue  →  Todo  →  In Progress  →  To Review  →  Sprint N Done
```

Convert the draft to an issue before starting, so the work has a number to reference from the branch and the PR.

### Pull requests

1. Push the branch and open a PR into `dev`; move the card to `To Review`.
2. Fill in the template and assign reviewers.
3. Collect **2 approvals**. Any new commit dismisses existing approvals, so push fixes before asking for the final review.
4. Resolve every conversation — it is enforced by branch protection.
5. Merge the PR yourself once the checks above are met.
6. Delete the branch manually (auto-delete is off) and move the card to `Sprint N Done`.

Before requesting review, sync with `dev` and resolve conflicts locally:

```bash
git switch dev && git pull
git switch feature/SSAD-42/add-partner-endpoint
git merge dev
```

### Hotfixes

Branch off `dev`, fix, and open a PR back into `dev` under the same rules. `main` receives changes only by merging `dev` as a release.

---

## Project layout

```
Streetcode/
├── Streetcode.WebApi/          controllers, DI and pipeline configuration, entry point
├── Streetcode.BLL/             business logic: MediatR handlers, DTOs, services
├── Streetcode.DAL/             EF Core entities, DbContext, migrations, repositories
├── Streetcode.XUnitTest/       unit tests
├── Streetcode.XIntegrationTest/integration tests
└── DbUpdate/                   DbUp runner for the raw SQL scripts in DAL/Persistence/ScriptsMigration
build/                          Nuke Build targets
```

---

## Known gaps

Inherited from the reference tree and left as is:

* The GitHub Actions workflows target `master`/`develop` and an upstream SonarCloud project whose token this repository does not hold. A red check on a PR is expected and does **not** block a merge — no status checks are required by branch protection.
* `.github/PULL_REQUEST_TEMPLATE/develop.md` and `master.md` are leftovers named after branches that no longer exist. GitHub uses `.github/pull_request_template.md`.
* The Nuke targets `SetupDocker` and `CleanDocker` call `docker-compose`, but no compose file ships with this repository.
* The `.editorconfig` referenced by the project files is absent.

---

## FAQ

**Visual Studio will not open the solution.**
Use a version of Visual Studio that supports .NET 10. The 2019 & 2022 release cannot load `net10.0` projects.

**Startup logs a migration error and every request fails.**
The database is unreachable — see [Database](#database). The host starts regardless, so the log is the only signal.

**Swagger returns 404.**
The application is running under a profile other than `Streetcode_Local`. Swagger is registered only for the `Local` environment.

**All endpoints return empty collections.**
Expected on a fresh database: seeding is disabled in `Program.cs`.

---

## License

* **[MIT license](http://opensource.org/licenses/mit-license.php)**
* Copyright 2022 © <a href="https://softserve.academy/" target="_blank">SoftServe IT Academy</a>.
