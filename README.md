# Protein Tracker API

Protein Tracker is a single-user ASP.NET Core Web API for maintaining reusable food definitions, recording consumed food, configuring daily macronutrient targets, and viewing daily nutrition summaries.

The backend exposes HTTP endpoints and persists data in PostgreSQL. It does not currently include a frontend, authentication, or multi-user data separation.

## Current functionality

- Create and edit foods with protein, carbohydrate, and fat values per 100 grams.
- Archive and restore foods. Archiving is a reversible soft-delete operation.
- Record, edit, query, and delete food entries with an amount and offset-aware consumption timestamp.
- Configure one current daily protein, carbohydrate, and fat target.
- Retrieve a daily summary containing consumed, target, and remaining nutrition.
- Explore and manually invoke the API through Swagger UI in development.

Archived foods cannot be selected for new or reassigned food entries. Existing entries may continue referencing an archived food, and their amount or timestamp can still be corrected.

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
| `ProteinTracker.Api/Swagger/` | OpenAPI metadata, endpoint documentation, and request examples |
| `ProteinTracker.Api/Migrations/` | EF Core PostgreSQL schema migrations |
| `ProteinTracker.Api.Tests/` | xUnit tests for calculations and service behavior |

Controllers are intentionally thin. They delegate validation, nutrition calculations, archive rules, and timezone behavior to services.

## Technology stack

- .NET 8 and ASP.NET Core Web API
- Entity Framework Core 8
- PostgreSQL through `Npgsql.EntityFrameworkCore.PostgreSQL`
- Swashbuckle Swagger/OpenAPI
- xUnit with EF Core InMemory for automated service tests

## API overview

| Group | Base route | Purpose |
| --- | --- | --- |
| Foods | `/api/foods` | List active or archived foods; get, create, update, archive, and restore food definitions |
| Food entries | `/api/food-entries` | Get entries, query an offset-aware timestamp range, create, update, and delete consumption records |
| Daily target | `/api/daily-target` | Read or update the single current macro target |
| Daily summary | `/api/daily-summary` | Get consumed, target, and remaining nutrition for a Bratislava calendar date |

Swagger contains the detailed routes, request examples, business behavior, and response documentation.

## Database model

The initial migration creates three application tables:

- `Foods`: reusable per-100g nutritional definitions and the `IsArchived` soft-archive flag.
- `FoodEntries`: consumed amounts and UTC timestamps linked to Foods.
- `DailyTargets`: the current macro-target persistence model.

`Food` has a one-to-many relationship with `FoodEntry`. `FoodEntry.FoodId` is required and indexed. The foreign key uses restrictive deletion, so a referenced Food cannot be physically deleted along with historical entries. Normal Food removal uses archive/restore operations; individual FoodEntries may be physically deleted.

Decimal nutrition and gram columns use PostgreSQL `numeric(10,3)` precision.

## Error handling

The global ASP.NET Core exception handler converts known application exceptions into standard `ProblemDetails` responses:

- Invalid business input and attempts to assign archived foods return HTTP 400.
- Missing foods or food entries return HTTP 404.
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
```

The environment variable overrides the value in `ProteinTracker.Api/appsettings.Development.json`.

`ProteinTracker.Api/appsettings.Development.example.json` contains a safe placeholder template. Never put real database credentials, API keys, tokens, or other secrets in tracked configuration files. Prefer environment variables or initialize .NET user-secrets locally:

```bash
dotnet user-secrets init --project ProteinTracker.Api/ProteinTracker.Api.csproj
dotnet user-secrets set --project ProteinTracker.Api/ProteinTracker.Api.csproj \
  'ConnectionStrings:ProteinTrackerDatabase' \
  'Host=localhost;Port=5432;Database=protein_tracker;Username=YOUR_USERNAME;Password=YOUR_PASSWORD'
```

User-secrets are stored outside the repository. Replace the placeholders only in your local command; do not commit the resulting secret value.

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

The tests cover nutrition calculations and the Food, FoodEntry, DailyTarget, and DailySummary service rules, including archive behavior, upserts, current-value nutrition, UTC normalization, and Bratislava day boundaries. They use EF Core InMemory for isolation and speed; this does not replace integration testing against PostgreSQL/Npgsql for provider-specific behavior.

## Swagger UI

Swagger is enabled only when the application runs in the Development environment. The checked-in launch profiles open the `swagger` path automatically:

- `https://localhost:7202/swagger` with the `https` profile
- `http://localhost:5132/swagger` with either project profile

Swagger UI includes endpoint descriptions, documented status codes, `ProblemDetails` responses, request examples, and a preconfigured daily-summary date example.
