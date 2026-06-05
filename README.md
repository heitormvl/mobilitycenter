# MicroMobilityHub

A crowdsourced map platform for micromobility infrastructure (bike racks, scooter stations, etc.). Users can discover, rate, and review bike parking facilities with granular filtering for services, access types, and supported vehicle types.

## 🎯 Features

### MVP (Phase 1)
- **Interactive map** with location-based filters (5-50km radius)
- **CRUD operations** for bike racks (crowdsourced + moderation)
- **Rating system** (1-5 stars + comments)
- **Granular filters:**
  - Services: power outlet, air pump, locker, storage, maintenance space, bike lock
  - Access: free, paid, requires sign-up, monthly subscription
  - Vehicles: bikes, scooters, monowheels, e-skates

### Phase 2
- Freemium monetization (operators pay for highlight/verification)
- User reputation system
- Real-time notifications for new facilities nearby

## 🏗️ Architecture

**4-layer clean architecture:**
- **API Layer** — Controllers, HTTP mappings, middleware
- **Business Layer** — Services, business logic, validation
- **Repository Layer** — EF Core DbContext, data access
- **Shared Layer** — Domain models, DTOs, enums, exceptions

## 🛠️ Tech Stack

| Component | Technology |
|-----------|-----------|
| Backend | .NET 10 + C# + ASP.NET Core + EF Core |
| Database | PostgreSQL 17 + PostGIS 3.5 (geospatial) |
| Frontend | Blazor WebAssembly / PWA |
| Infrastructure | Docker, GitHub Actions |
| ORM | Entity Framework Core 10 |

## 📦 Project Structure

```
MobilityCenter/
├── src/
│   ├── MobilityCenter.API/              # ASP.NET Core API
│   ├── MobilityCenter.Business/         # Services & business logic
│   ├── MobilityCenter.Repositories/     # EF Core & data access
│   └── MobilityCenter.Shared/           # Models, DTOs, enums
├── docker-compose.yml                   # PostgreSQL + PostGIS
├── MobilityCenter.slnx                  # Solution file
└── CLAUDE.md                            # Development guide for Claude Code
```

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK
- Docker & Docker Compose
- Git

### Local Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/heitormvl/mobilitycenter.git
   cd mobilitycenter
   ```

2. **Start PostgreSQL + PostGIS**
   ```bash
   docker compose up -d
   ```
   
   Database will be available at:
   - Host: `localhost`
   - Port: `5432`
   - Database: `mobilitycenter`
   - User: `mc_user`
   - Password: `mc_dev_password`

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Build solution**
   ```bash
   dotnet build
   ```

5. **Run migrations** (when ready)
   ```bash
   dotnet ef migrations add InitialCreate -p ./src/MobilityCenter.Repositories -s ./src/MobilityCenter.API
   dotnet ef database update -p ./src/MobilityCenter.Repositories -s ./src/MobilityCenter.API
   ```

6. **Start API**
   ```bash
   dotnet run --project ./src/MobilityCenter.API
   ```
   
   API will be available at:
   - HTTP: `http://localhost:5000`
   - HTTPS: `https://localhost:7000`

## 🔧 Development

### Common Commands

```bash
# Build & run
dotnet build
dotnet build -c Release
dotnet run --project ./src/MobilityCenter.API
dotnet watch run --project ./src/MobilityCenter.API

# Testing
dotnet test
dotnet test --filter "ClassName.MethodName"

# Formatting
dotnet format

# Database
dotnet ef migrations add MigrationName -p ./src/MobilityCenter.Repositories -s ./src/MobilityCenter.API
dotnet ef database update -p ./src/MobilityCenter.Repositories -s ./src/MobilityCenter.API
dotnet ef database drop -p ./src/MobilityCenter.Repositories -s ./src/MobilityCenter.API
```

### CORS Configuration

**Development** (`appsettings.Development.json`):
- Allows requests from `http://localhost:3000`, `http://localhost:4200`, `http://localhost:5173`, `https://localhost:7001`
- Allows any header and method
- Credentials enabled

**Production** (`appsettings.Production.json`):
- Whitelist specific origins (configure via environment variables)
- Restricted to necessary headers: `Content-Type`, `Authorization`
- Allowed methods: `GET`, `POST`, `PUT`, `PATCH`, `DELETE`

### Environment Configuration

Settings are loaded in this order (later values override earlier):
1. `appsettings.json` (base)
2. `appsettings.{ASPNETCORE_ENVIRONMENT}.json` (dev/prod)
3. Environment variables (CI/CD, Docker)

**Development:** Connection string and JWT secret are committed (safe defaults).
**Production:** All sensitive values should come from environment variables or Azure Key Vault.

## 🗄️ Database Schema

### Core Models

**Bicicletario** (Bike Rack)
- `Id` (Guid)
- `Name` (string)
- `Latitude`, `Longitude` (decimal)
- `Location` (PostGIS Point, SRID 4326)
- Service flags: `HasPowerOutlet`, `HasAirPump`, `HasLocker`, `HasStorage`, `HasMaintenanceSpace`, `HasBikeLock`
- Access type: `IsFree`, `IsPaid`, `RequiresSignup`, `IsMonthlySubscription`
- `VehicleTypes` (enum flags)
- `OperatorId` (FK to Usuario)
- `Ratings` (collection of Avaliacao)
- `CreatedAt`, `UpdatedAt`, `IsDeleted`

**Usuario** (User)
- `Id` (Guid)
- `Name` (string)
- `Email` (string, unique)
- `UserType` (enum: Usuario, Operador, Admin)
- `CreatedAt`, `IsActive`

**Avaliacao** (Rating)
- `Id` (Guid)
- `BicicletarioId` (FK)
- `UsuarioId` (FK)
- `Rating` (1-5)
- `Comment` (string, optional)
- `CreatedAt`

## 🔐 Authentication

JWT-based authentication (configured in `appsettings.{environment}.json`):
- Issuer: `MobilityCenter`
- Audience: `MobilityCenter`
- Secret: Set via environment variable in production

## 📝 Contributing

1. Create a feature branch: `git checkout -b feature/my-feature`
2. Commit changes: `git commit -m "description"`
3. Push to remote: `git push origin feature/my-feature`
4. Create a pull request

## 📄 License

[Specify your license here]

## 👤 Author

[Your name/contact]
