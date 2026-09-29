# Template API

A .NET 10 Minimal API template with one small worked example: users register, then create and read products with HTTP Basic authentication.

| Concern | Choice |
| --- | --- |
| API style | Minimal APIs, one `IEndpoint` class per endpoint |
| Commands / queries | Own `ICommandHandler` / `IQueryHandler` interfaces. No MediatR |
| Pipeline behaviours | Decorators (logging, then validation) registered with Scrutor |
| Responses | `Result<T>` + `Error`, mapped to RFC 9457 problem details |
| Validation | FluentValidation, run by the validation decorator |
| Mapping | Hand-written `ToCommand()` / `ToResponse()` methods. No mapping library |
| Versioning | `Asp.Versioning`, URL segment (`/api/v1/...`) |
| API docs | Built-in OpenAPI + Scalar (`/scalar`), one document per version |
| Authentication | HTTP Basic over HTTPS, PBKDF2 password hashes, per-IP rate limiting |
| Database | SQL Server + EF Core, code-first migrations |
| Cache | `HybridCache`: in-memory first, Redis second |
| Logging | Serilog (console + OTLP) |
| Observability | OpenTelemetry traces, metrics and logs sent to the standalone Aspire dashboard |
| Packages | Central Package Management (`Directory.Packages.props`) |
| Tests | xUnit v3 + Shouldly + NSubstitute |
| Secrets | `dotnet user-secrets` locally, gitignored `.env` for Docker. Nothing in `appsettings*.json` |

## 1. Prerequisites

- .NET 10 SDK
- Docker Desktop (running)
- PowerShell 7+ (`pwsh`), used by every script and available on Windows, macOS and Linux

Check your machine:

```powershell
./scripts/check-prerequisites.ps1
```

Each failed check prints the fix.

## 2. Create local secrets (once)

```powershell
./scripts/setup-secrets.ps1
```

This script:

1. Writes `.env` with generated passwords for SQL Server, Redis, the HTTPS certificate and the Aspire dashboard login.
2. Stores the SQL Server and Redis connection strings in `dotnet user-secrets` for the API project.
3. Trusts the ASP.NET Core HTTPS dev certificate (may show a confirmation dialog) and exports it to `.certs/` for the API container.

Running it again keeps the existing values. `.env` and `.certs/` are gitignored.

## 3. Run

### Option A: API on your machine, dependencies in Docker (daily development)

```powershell
./scripts/docker-up.ps1 -InfraOnly
dotnet run --project src/Template.Api
```

| URL | What |
| --- | --- |
| https://localhost:7294/scalar | Scalar API reference |
| https://localhost:7294/openapi/v1.json | OpenAPI document |
| http://localhost:18888/login?t=&lt;DASHBOARD_BROWSER_TOKEN&gt; | Aspire dashboard (`docker-up.ps1` prints the full link) |

### Option B: everything in Docker

```powershell
./scripts/docker-up.ps1
```

The API runs on https://localhost:8443 (Scalar at `/scalar`).

In both options the database is created and migrated on startup in the `Development` environment.

Stop the containers:

```powershell
./scripts/docker-down.ps1            # keep data
./scripts/docker-down.ps1 -Volumes   # delete the SQL Server data too
```

## 4. Try it

```powershell
# Register (anonymous)
curl.exe -X POST https://localhost:7294/api/v1/users -H "Content-Type: application/json" `
  -d '{\"username\":\"alice\",\"password\":\"correct-horse-battery\"}'

# Create a product (Basic auth)
curl.exe -u alice:correct-horse-battery -X POST https://localhost:7294/api/v1/products `
  -H "Content-Type: application/json" -d '{\"name\":\"Keyboard\",\"price\":49.99}'

# Read it. The second call is served from the cache.
curl.exe -u alice:correct-horse-battery https://localhost:7294/api/v1/products/<id>
```

In Scalar, click **Authentication**, enter the username and password, then use **Test Request**.

Open the Aspire dashboard to see the request traces, the SQL queries and the Serilog logs for each call.

## 5. Inspect SQL Server and Redis

The passwords are in `.env`:

```powershell
Get-Content .env
```

Connect from the host with `127.0.0.1`, not `localhost`. The containers listen on IPv4 only, and `localhost` can resolve to IPv6 (`::1`) first, which makes clients time out.

### SQL Server

Connection details for any GUI client (Rider or Visual Studio database explorer, SSMS, or the VS Code "SQL Server (mssql)" extension):

| Setting | Value |
| --- | --- |
| Server | `127.0.0.1,1433` (JDBC clients such as Rider: host `127.0.0.1`, port `1433`) |
| Authentication | SQL Server authentication |
| Login | `sa` |
| Password | `SQL_SA_PASSWORD` from `.env` |
| Database | `TemplateDb` |
| Trust server certificate | Yes (the container uses a self-signed certificate) |

Tables: `Products`, `Users` and `__EFMigrationsHistory` (applied migrations). `Users.PasswordHash` holds PBKDF2 hashes, never plain passwords.

From the command line, with no local tools needed:

```powershell
# Interactive sqlcmd session inside the container
docker compose exec sqlserver sh -c '/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -d TemplateDb'
```

```sql
SELECT name FROM sys.tables;
GO
SELECT * FROM Products;
GO
SELECT MigrationId FROM __EFMigrationsHistory;
GO
EXIT
```

Run a single query without an interactive session:

```powershell
docker compose exec sqlserver sh -c '/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -d TemplateDb -Q "SELECT * FROM Products"'
```

### Redis

Connection details for a GUI client (Redis Insight, free, or the Rider/DataGrip Redis driver):

| Setting | Value |
| --- | --- |
| Host | `127.0.0.1` |
| Port | `6379` |
| Username | `default` (or leave empty) |
| Password | `REDIS_PASSWORD` from `.env` |

From the command line:

```powershell
# Interactive redis-cli session inside the container, already authenticated
docker compose exec redis sh -c 'redis-cli -a "$REDIS_PASSWORD" --no-auth-warning'
```

```text
SCAN 0 MATCH template:* COUNT 100            # list cache keys (use SCAN, not KEYS, outside local dev)
TYPE template:products:<id>                  # hash
TTL template:products:<id>                   # seconds left, up to 300
HGETALL template:products:<id>               # the cached entry
DEL template:products:<id>                   # evict one entry
FLUSHDB                                      # clear the whole cache
```

How the cache is stored:

- Every key starts with `template:`, which is the `InstanceName` in `Infrastructure/DependencyInjection.cs`, followed by the `HybridCache` key, for example `template:products:<id>`.
- Each key is a hash with three fields. `data` holds the payload: a short binary `HybridCache` header followed by the JSON product (or `null` for a cached "not found"). `absexp` is the absolute expiry and `sldexp` the sliding expiry (`-1` means none).
- Entries expire after 5 minutes in Redis and after 1 minute in the API's memory. After a `DEL`, a running API can still serve the entry from memory for up to that 1 minute.

## 6. Project structure

```
src/
  Template.Domain/          Entities, Result<T>, Error. No dependencies.
  Template.Application/     Commands, queries, handlers, validators, decorators, repository interfaces.
    Abstractions/           ICommand, IQuery, handler interfaces, repository and hasher interfaces
    Behaviors/              LoggingDecorator, ValidationDecorator
    Features/<Area>/        One file per use case: command/query + validator + handler
  Template.Infrastructure/  EF Core DbContext, migrations, repositories, Redis/HybridCache, password hasher
  Template.Api/             Program.cs, endpoints, Basic auth, OpenAPI/Scalar, Serilog/OpenTelemetry
    Configurations/         One static class per concern (Observability, OpenApi, Security, Endpoints)
    Endpoints/<Area>/       One file per endpoint: request record + ToCommand() + IEndpoint
tests/
  Template.UnitTests/       Handler, validator, decorator and DI wiring tests
scripts/                    Prerequisite check, secrets setup, Docker helpers
```

## 7. How a request flows

```
HTTP request
  -> Rate limiter -> Basic authentication (AuthenticateUserQuery) -> Authorization
  -> Endpoint: request.ToCommand()
  -> LoggingDecorator      logs name, outcome, duration (never the payload)
  -> ValidationDecorator   runs FluentValidation, returns a Validation error on failure
  -> Handler               returns Result<T>
  -> Endpoint: result.Match(success => Results.Ok/Created, error => error.ToProblem())
```

Error types map to status codes in `Template.Api/Endpoints/ErrorExtensions.cs`: Validation 400, Unauthorized 401, NotFound 404, Conflict 409, Failure 500. Unhandled exceptions become a 500 problem details response and are logged.

## 8. Add a feature

1. **Application**: add `Features/<Area>/<UseCase>.cs` with a `sealed record` command or query, an optional `AbstractValidator`, and a `sealed` handler. Handlers and validators are registered automatically, and the decorators apply to them.
2. **Api**: add `Endpoints/<Area>/<UseCase>.cs` with a request record, a `ToCommand()` method and a `sealed class ... : IEndpoint`. Endpoints are registered automatically.
3. **Tests**: add handler and validator tests under `tests/Template.UnitTests/Features/<Area>`.

## 9. Database migrations

Code-first with a pinned local `dotnet-ef` tool (`.config/dotnet-tools.json`).

```powershell
dotnet tool restore

dotnet ef migrations add <Name> `
  --project src/Template.Infrastructure `
  --startup-project src/Template.Api `
  --output-dir Persistence/Migrations
```

Migrations are applied on startup in `Development`. For other environments, apply them from a pipeline with `dotnet ef database update` or a migration bundle (`dotnet ef migrations bundle`).

## 10. API versioning

Every endpoint lives under `api/v{version}` and calls `.MapToApiVersion(n)`. To add v2:

1. Add `new ApiVersion(2)` to the version set in `Configurations/Endpoints.cs`.
2. Map the new endpoint with `.MapToApiVersion(2)`.
3. The v2 OpenAPI document appears at `/openapi/v2.json` with no extra setup.

## 11. Tests

```powershell
dotnet test
```

Tests run on the Microsoft Testing Platform (opted in via `global.json`). `HybridCache` tests use the real in-memory cache, with no Redis.

## 12. Scripts

| Script | Purpose |
| --- | --- |
| `scripts/check-prerequisites.ps1` | Verifies the SDK, dotnet-ef, HTTPS cert, Docker, Compose and `.env`. No parameters. |
| `scripts/setup-secrets.ps1` | Creates `.env`, user secrets and the container HTTPS certificate. No parameters. |
| `scripts/docker-build.ps1` | Builds the `template-api` image |
| `scripts/docker-up.ps1` | Starts the containers |
| `scripts/docker-down.ps1` | Stops the containers |

### docker-build.ps1

| Parameter | Type | Default | Effect |
| --- | --- | --- | --- |
| `-Tag <value>` | string | `latest` | Image tag. Builds `template-api:<value>`. |

```powershell
./scripts/docker-build.ps1              # template-api:latest
./scripts/docker-build.ps1 -Tag 1.2.0   # template-api:1.2.0
```

`docker-compose.yml` runs `template-api:latest`, so a custom tag is not used by `docker-up.ps1`.

### docker-up.ps1

| Parameter | Type | Default | Effect |
| --- | --- | --- | --- |
| `-InfraOnly` | switch | off | Starts only `sqlserver`, `redis` and `aspire-dashboard`. Use it when the API runs with `dotnet run`. |

```powershell
./scripts/docker-up.ps1              # all four containers, rebuilds the API image first
./scripts/docker-up.ps1 -InfraOnly   # dependencies only, no API image build
```

The script stops with an error if `.env` is missing. It waits until SQL Server and Redis report healthy, then prints the Aspire dashboard login link and the Scalar URL.

### docker-down.ps1

| Parameter | Type | Default | Effect |
| --- | --- | --- | --- |
| `-Volumes` | switch | off | Also deletes the `sqlserver-data` volume. All database data is lost. |

```powershell
./scripts/docker-down.ps1            # stop and remove containers, keep data
./scripts/docker-down.ps1 -Volumes   # stop, remove containers and delete the database
```

Use `-Volumes` after changing `SQL_SA_PASSWORD` in `.env`. SQL Server keeps the password from its first start inside the volume.

## 13. Build pipeline (Azure DevOps)

`infra/azure-pipelines.yml` runs on every push and pull request to `main`, on an `ubuntu-latest` agent:

1. Installs the .NET SDK version from `global.json`.
2. Restores and builds `Template.slnx` in Release.
3. Runs the unit tests and publishes the results to the pipeline's **Tests** tab.
4. Builds the API Docker image, tagged `template-api:<BuildId>` and `template-api:latest`. The image is not pushed.

Set it up once: **Pipelines > New pipeline**, pick the repository, choose **Existing Azure Pipelines YAML file**, then select `/infra/azure-pipelines.yml`.

To push the image, create a Docker registry service connection and change the Docker step to `command: buildAndPush` with `containerRegistry: <service connection name>`.

## 14. Security notes and known limits

- Basic credentials are sent on every request. The handler rejects them over plain HTTP. Behind a TLS-terminating proxy, add `UseForwardedHeaders` so `Request.IsHttps` stays correct.
- Every authenticated request runs a PBKDF2 verification (100,000 iterations) and one database lookup. This is intentional for security and costs a few milliseconds per call.
- Unknown usernames take the same time to reject as wrong passwords.
- Rate limiting is 60 requests per minute per client IP, set in `Configurations/Security.cs`. There is no per-account lockout.
- The Docker stack uses the SQL Server `sa` login and the `Development` environment. It is for local use only.
- `GetProductById` caches "not found" results for up to 5 minutes. Product ids are generated by the server, so an id cannot exist before it is created.
