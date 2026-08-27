# Protein Tracker

Protein Tracker is an authenticated ASP.NET Core and React application for maintaining reusable food definitions, recording consumed food, configuring daily macronutrient targets, and viewing daily nutrition summaries.

The backend exposes JWT-protected HTTP endpoints and persists data in PostgreSQL. Each account has an isolated food library, entries, target, and summaries. The repository also contains a React frontend for the core tracking workflows.

## Current functionality

- Create and edit foods with protein, carbohydrate, and fat values per 100 grams.
- Archive and restore foods. Unused archived foods may also be permanently deleted.
- Record, edit, query, and delete food entries with an amount and offset-aware consumption timestamp.
- Configure one current daily protein, carbohydrate, and fat target.
- Retrieve a daily summary containing consumed, target, and remaining nutrition.
- Explore and manually invoke the API through Swagger UI in development.
- Register, log in, log out, and access only the nutrition data owned by the authenticated account.

Archived foods cannot be selected for new or reassigned food entries. Existing entries may continue referencing an archived food, and their amount or timestamp can still be corrected. Permanent deletion is limited to archived foods with no historical FoodEntry references.

## Nutrition and historical calculations

Calories are calculated rather than stored:

```text
Calories = Protein × 4 + Carbohydrates × 4 + Fat × 9
```

Food values are stored per 100g. An entry's consumed macros are calculated as:

```text
Consumed macro = Macro per 100g × Amount in grams ÷ 100
```

Food entries do not contain nutritional snapshots. Responses and daily summaries use the referenced Food's current nutritional values. Correcting a Food therefore changes recalculated nutrition for historical entries.

Daily target calories are also calculated from the three macro targets and are not persisted. Remaining nutrition is `Target - Consumed`; negative values indicate that a target was exceeded.

## Time handling

Clients may submit `ConsumedAt` as a `DateTimeOffset` with any valid offset, such as `2026-08-26T12:30:00+02:00`. The service preserves the represented instant and normalizes the value to UTC (`+00:00`) before PostgreSQL persistence.

Food-entry range endpoints accept resolved `DateTimeOffset` boundaries and use them directly. Daily summaries instead accept a local calendar date in `yyyy-MM-dd` format. The summary service interprets that date in the configured `Europe/Bratislava` timezone, converts consecutive local midnights to UTC, and queries the half-open range `[start, end)`.

## Architecture

The request flow is:

```text
Controller → Service → Repository → EF Core → PostgreSQL
```

| Area | Responsibility |
| --- | --- |
| `ProteinTracker.Api/Controllers/` | Attribute-routed HTTP endpoints and status-code responses |
| `ProteinTracker.Api/Services/` | Validation, business rules, mapping, aggregation, and timezone handling |
| `ProteinTracker.Api/Repositories/` | Asynchronous EF Core queries and persistence |
| `ProteinTracker.Api/Data/` | `ProteinTrackerDbContext` and entity configuration |
| `ProteinTracker.Api/Models/` | Persisted domain entities |
| `ProteinTracker.Api/DTOs/` | HTTP request and response contracts |
| `ProteinTracker.Api/Utils/` | Pure reusable nutrition calculations |
| `ProteinTracker.Api/Exceptions/` | Business exceptions and centralized `ProblemDetails` handling |
| `ProteinTracker.Api/Security/` | Current-user claim access and JWT configuration |
| `ProteinTracker.Api/Swagger/` | OpenAPI metadata, endpoint documentation, and request examples |
| `ProteinTracker.Api/Migrations/` | EF Core PostgreSQL schema migrations |
| `ProteinTracker.Api.Tests/` | xUnit tests for calculations and service behavior |
| `ProteinTracker.Web/` | React, TypeScript, and Vite frontend with a typed API client |

Controllers are intentionally thin. They delegate validation, nutrition calculations, archive rules, and timezone behavior to services. JWT middleware establishes identity, while repositories enforce ownership in every application-data query.

## Technology stack

- .NET 8 and ASP.NET Core Web API
- Entity Framework Core 8
- PostgreSQL through `Npgsql.EntityFrameworkCore.PostgreSQL`
- Swashbuckle Swagger/OpenAPI
- ASP.NET Core JWT bearer authentication and password hashing
- xUnit with EF Core InMemory for automated service tests
- React 19, TypeScript, and Vite for the web application
- React Router for client-side routes
- Docker Compose, nginx, and PostgreSQL for the complete local container stack

## API overview

| Group | Base route | Purpose |
| --- | --- | --- |
| Foods | `/api/foods` | List active or archived foods; get, create, update, archive, restore, and delete eligible archived food definitions |
| Food entries | `/api/food-entries` | Get entries, query an offset-aware timestamp range, create, update, and delete consumption records |
| Daily target | `/api/daily-target` | Read or update the single current macro target |
| Daily summary | `/api/daily-summary` | Get consumed, target, and remaining nutrition for a Bratislava calendar date |
| Authentication | `/api/auth` | Register or log in and receive a JWT bearer token |

Swagger contains the detailed routes, request examples, business behavior, and response documentation.

## Database model

The migrations create four application tables:

- `Foods`: reusable per-100g nutritional definitions and the `IsArchived` soft-archive flag.
- `FoodEntries`: consumed amounts and UTC timestamps linked to Foods.
- `DailyTargets`: the current macro-target persistence model.
- `Users`: normalized unique emails, password hashes, and account creation timestamps.

Foods, FoodEntries, and DailyTargets carry required User ownership. Every repository query includes the authenticated User ID. FoodEntry uses a composite `(FoodId, UserId)` foreign key, preventing cross-user Food assignment at the database layer as well. DailyTarget has one unique row per User.

The authentication/ownership migration intentionally deletes legacy Foods, FoodEntries, and DailyTargets because those older rows have no trustworthy owner. It never assigns existing private data to an arbitrary account. The existing Food-to-FoodEntry restrictive deletion behavior remains in place.

Decimal nutrition and gram columns use PostgreSQL `numeric(10,3)` precision.

## Error handling

The global ASP.NET Core exception handler converts known application exceptions into standard `ProblemDetails` responses:

- Invalid business input and attempts to assign archived foods return HTTP 400.
- Missing foods or food entries return HTTP 404.
- Attempts to delete Foods referenced by historical entries return HTTP 409.
- Missing or invalid bearer tokens return HTTP 401.
- Unexpected failures return HTTP 500 with generic client-facing details; internal exception details are not exposed.

Controllers allow these exceptions to reach the centralized handler.

## Local development

### Prerequisites

- .NET 8 SDK
- PostgreSQL listening on `localhost:5432`
- A PostgreSQL database named `protein_tracker`
- EF Core CLI tooling compatible with EF Core 8

Install the EF CLI if `dotnet ef --version` is unavailable:

```bash
dotnet tool install --global dotnet-ef --version 8.*
```

### Restore dependencies

From the repository root:

```bash
dotnet restore ProteinTracker.sln
```

### Configure PostgreSQL

The application reads the `ProteinTrackerDatabase` connection string. The safe development example targets:

```text
Host=localhost;Port=5432;Database=protein_tracker
```

Supply your own PostgreSQL username and password. Do not commit real credentials. One option is an environment variable:

```bash
export ConnectionStrings__ProteinTrackerDatabase='Host=localhost;Port=5432;Database=protein_tracker;Username=YOUR_USERNAME;Password=YOUR_PASSWORD'
export Jwt__SigningKey='A_RANDOM_LOCAL_SECRET_AT_LEAST_32_CHARACTERS_LONG'
```

The environment variable overrides the value in `ProteinTracker.Api/appsettings.Development.json`.

`ProteinTracker.Api/appsettings.Development.example.json` contains a safe placeholder template. Never put real database credentials, API keys, tokens, or other secrets in tracked configuration files. Prefer environment variables or initialize .NET user-secrets locally:

```bash
dotnet user-secrets init --project ProteinTracker.Api/ProteinTracker.Api.csproj
dotnet user-secrets set --project ProteinTracker.Api/ProteinTracker.Api.csproj \
  'ConnectionStrings:ProteinTrackerDatabase' \
  'Host=localhost;Port=5432;Database=protein_tracker;Username=YOUR_USERNAME;Password=YOUR_PASSWORD'
dotnet user-secrets set --project ProteinTracker.Api/ProteinTracker.Api.csproj \
  'Jwt:SigningKey' \
  'A_RANDOM_LOCAL_SECRET_AT_LEAST_32_CHARACTERS_LONG'
```

User-secrets are stored outside the repository. Replace the placeholders only in your local command; do not commit the resulting secret value.

## Docker Compose

Docker Compose runs the production frontend, API, and an isolated PostgreSQL database. From the repository root:

```bash
cp .env.example .env
```

Replace the database placeholders and `JWT_SIGNING_KEY` with local Docker-only secrets, then start the stack:

```bash
docker compose up --build
```

Open `http://localhost:8080`. nginx serves the React application, falls back to `index.html` for `/`, `/foods`, and `/targets`, and proxies same-origin `/api` requests to the API container. PostgreSQL is available to host tools at `localhost:5433`; containers connect to it as `db:5432`.

The API waits for PostgreSQL's health check and applies the existing EF Core migrations at startup because Compose sets `Database__MigrateOnStartup=true`. This flag is disabled by default outside Compose. Database files live in the `protein_tracker_postgres` named volume and survive normal container recreation.

Stop the stack without deleting data:

```bash
docker compose down
```

To deliberately remove the local Docker database as well, use `docker compose down --volumes`. This permanently deletes the Compose-managed data volume.

The `.env` file is ignored by Git. Never commit real database credentials, JWT signing keys, or other secrets; `.env.example` contains placeholders only.

## Frontend authentication

The frontend protects `/`, `/foods`, and `/targets`; unauthenticated visitors are redirected to `/login`. Registration is available at `/register`. The centralized API client adds the bearer token to requests and clears the session on HTTP 401 so expired or invalid sessions return to login.

For this local MVP, the JWT and its expiry are stored in `localStorage`. This keeps login state across refreshes and works with the same-origin nginx proxy, but JavaScript-accessible storage can expose tokens if an XSS vulnerability exists. A production deployment should prefer short-lived access tokens held in memory with refresh credentials in `Secure`, `HttpOnly`, `SameSite` cookies, alongside an appropriate CSRF strategy and a hardened content-security policy.

### Apply migrations

The repository contains the `InitialCreate` migration. Apply it with development configuration enabled:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update \
  --project ProteinTracker.Api/ProteinTracker.Api.csproj \
  --startup-project ProteinTracker.Api/ProteinTracker.Api.csproj
```

This updates only the configured `protein_tracker` database. No seed data is included.

### Run the API

```bash
dotnet run --project ProteinTracker.Api/ProteinTracker.Api.csproj --launch-profile https
```

The HTTPS launch profile listens on `https://localhost:7202` and also exposes `http://localhost:5132`. The HTTP-only profile can be started with:

```bash
dotnet run --project ProteinTracker.Api/ProteinTracker.Api.csproj --launch-profile http
```

### Run tests

```bash
dotnet test ProteinTracker.sln
```

The tests cover registration, login, password hashing, unauthenticated HTTP rejection, cross-user ownership boundaries, and the Food, FoodEntry, DailyTarget, and DailySummary rules. This includes cross-user FoodEntry protection, archive behavior, upserts, current-value nutrition, UTC normalization, and Bratislava day boundaries. Most persistence tests use EF Core InMemory for isolation and speed; this does not replace integration testing against PostgreSQL/Npgsql for provider-specific behavior.

### Run the frontend

The current Vite version requires Node.js `^20.19.0` or `>=22.12.0`. From the repository root:

```bash
cd ProteinTracker.Web
cp .env.example .env.local  # optional local overrides
npm install
npm run dev
```

Vite normally serves the app at `http://localhost:5173` and proxies `/api` to `http://localhost:5132`. Set `VITE_API_PROXY_TARGET` in `.env.local` if the API uses a different local address. Use `npm run lint` and `npm run build` for frontend verification.

## Swagger UI

Swagger is enabled only when the application runs in the Development environment. The checked-in launch profiles open the `swagger` path automatically:

- `https://localhost:7202/swagger` with the `https` profile
- `http://localhost:5132/swagger` with either project profile

Swagger UI includes endpoint descriptions, documented status codes, `ProblemDetails` responses, request examples, and JWT Bearer authorization support. Register or log in, copy the returned token into Swagger's Authorize dialog, then exercise the protected endpoints.
