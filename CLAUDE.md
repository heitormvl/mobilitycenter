# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**MicroMobilityHub** is a crowdsourced map platform for micromobility infrastructure (bike racks, scooter stations, etc.). Users can discover, rate, and review bike parking facilities with granular filtering for services, access types, and supported vehicle types. Operators can claim and manage their locations.

## Technology Stack

- **Backend:** .NET 10 + C# + ASP.NET Core + EF Core
- **Database:** PostgreSQL with PostGIS (geospatial queries)
- **Frontend:** Blazor WebAssembly (web) or PWA
- **Infrastructure:** Docker, GitHub Actions, Azure or self-hosted
- **ORM:** Entity Framework Core with spatial types

## Architecture

The codebase follows a **4-layer clean architecture**:

1. **API Layer** — Controllers, HTTP mappings, middleware, request/response handling
2. **Business Layer** — Services, business logic, validation rules
3. **Repository Layer** — EF Core DbContext, Data Access, database queries
4. **Shared Layer** — Domain models, DTOs, enums, exceptions, constants

**Key Principle:** Dependencies flow inward. Controllers → Services → Repositories → EF Core. Shared types are used across all layers.

### Core Domain Models

- **Bicicletario** — Bike rack location: id, name, lat/lon, 6 service flags (tomada, calibrador, vestiário, armário, espaço manutenção, cadeado), 4 access fields (livre, pago, cadastro, mensal), vehicle types, operator_id, ratings collection
- **Usuario** — User account: id, name, email, user type (Operator/User/Admin)
- **Avaliacao** — Rating/review: id, bicicletario_id, usuario_id, star rating (1-5), comment

PostGIS enables efficient spatial queries (e.g., "find all racks within 50km of my location").

## Common Development Commands

### Setup & Build
```bash
dotnet restore                    # Restore NuGet packages
dotnet build                      # Build solution in Debug mode
dotnet build -c Release           # Build Release configuration
```

### Running & Debugging
```bash
dotnet run --project ./API       # Run API server
dotnet watch run --project ./API # Run with hot reload
```

### Database & Migrations
```bash
dotnet ef migrations add MigrationName -p ./Repositories -s ./API
dotnet ef database update -p ./Repositories -s ./API
dotnet ef database drop -p ./Repositories -s ./API  # Destructive
```

### Testing
```bash
dotnet test                                    # Run all tests
dotnet test --filter "ClassName.MethodName"  # Run specific test
dotnet test --verbosity detailed              # Verbose output
dotnet test --logger "console;verbosity=detailed"  # Detailed console output
```

### Linting & Code Quality
```bash
dotnet format                     # Auto-format code
dotnet format --verify-no-changes # Check formatting without changing
```

### Blazor Frontend (if using WASM)
```bash
dotnet run --project ./Frontend  # Run Blazor WASM app
```

## Key Architectural Patterns

### Repository Pattern
Services depend on `IRepository<T>` abstractions. Repositories encapsulate EF Core queries. Always use repositories, never inject DbContext directly into services.

### Geospatial Queries
PostGIS integration via EF Core `DbGeometry` or `Point` types. Example:
```csharp
var nearby = dbContext.Bicicletarios
    .Where(b => b.Location.Distance(userLocation) <= maxDistance)
    .ToList();
```

### Service-based Validation
Validation rules belong in the Business layer, not controllers. Use FluentValidation or custom validators before persisting.

### DTOs for API Contracts
Separate API DTOs from domain models. Use AutoMapper or manual mapping in service methods to avoid exposing internal details.

## Important Implementation Details

- **Soft Deletes:** Bicicletarios likely support soft deletes (IsDeleted flag) for data preservation
- **Audit Trail:** Usuario_id and timestamps on ratings for accountability
- **Operator Verification:** Only verified operators can claim/modify locations
- **Filtering Logic:** Services layer handles complex multi-field filtering (services, access type, vehicle compatibility)
- **Spatial Indexes:** PostgreSQL indices on geospatial columns for query performance

## Gotchas & Conventions

- Always use EF Core's `SaveChangesAsync()` for async database operations
- PostGIS queries must use SRID 4326 (WGS84) for consistency
- DTOs should never contain back-references to avoid circular serialization
- API responses should use consistent error format (see Shared/Exceptions)
- User authentication likely via JWT; validate claims in Authorization attributes

## When Adding New Features

1. **Start with the model** — Add domain class to Shared layer
2. **Create repository interface** — `IRepository<YourModel>` in Business layer
3. **Implement repository** — EF Core queries in Repository layer
4. **Add service method** — Business logic in Service class
5. **Create API endpoint** — Controller action with DTO mapping
6. **Write tests** — Unit test service logic, integration test repository queries
7. **Run migration** — `dotnet ef migrations add` and `dotnet ef database update`

## Testing Strategy

- **Unit Tests:** Service layer logic, validators, mappers (mock repositories)
- **Integration Tests:** Repository queries against a test database, full API endpoints
- **Use TestContainers** (if added) to spin up PostgreSQL for integration tests

## Docker & Deployment

- Dockerfile should build the .NET app, set up database migrations in startup
- docker-compose.yml likely includes PostgreSQL + PostGIS service
- GitHub Actions CI/CD runs tests, builds Docker image, deploys to Azure or self-hosted

## Debugging Tips

- Use `dotnet user-secrets` for storing local connection strings and API keys
- Enable EF Core query logging: `optionsBuilder.LogTo(Console.WriteLine)`
- Blazor WASM debugging: Use browser DevTools Console and Network tabs
- Database: `psql` CLI or pgAdmin to inspect queries and spatial data

---

**Last Updated:** When this file was created, no code existed yet. Update this document as the architecture evolves.
