---
name: run-mobilityCenter
description: Build, launch, and test the MobilityCenter API with PostgreSQL database
---

# Run MobilityCenter

MobilityCenter is a .NET 10 ASP.NET Core API with a Blazor WebAssembly frontend and PostgreSQL+PostGIS database. This skill builds and launches the API server on `http://localhost:5000` with automated database setup and migrations.

## Prerequisites

**System Requirements:**
- Windows with PowerShell 5.1+
- Docker and Docker Compose installed and running
- .NET 10 SDK
- Git (to clone the repo)

**Install Docker** (if not already installed):
```bash
# On Windows, install Docker Desktop from https://www.docker.com/products/docker-desktop
# Verify: docker --version
```

**Verify .NET SDK:**
```bash
dotnet --version
```

## Build

Build the solution and prepare the database:

```powershell
cd C:\Users\heito\source\repos\MobilityCenter
.\.claude\skills\run-mobilityCenter\driver.ps1 -Action build
```

This command:
1. Starts PostgreSQL 17 + PostGIS 3.5 via Docker Compose
2. Runs `dotnet build` to compile all projects
3. Applies database migrations via Entity Framework Core

**Time:** ~15-30 seconds on first run, ~5 seconds on subsequent runs (if containers are running).

## Run (Agent Path)

Launch the API using the driver script:

```powershell
cd C:\Users\heito\source\repos\MobilityCenter
.\.claude\skills\run-mobilityCenter\driver.ps1 -Action start -Wait 15
```

**Output:** 
- API runs on `http://localhost:5000`
- Scalar API documentation: `http://localhost:5000/scalar/v1`
- Database: PostgreSQL on `localhost:5432` (credentials in `docker-compose.yml`)

**Commands the driver accepts:**
- `-Action build` — Build and migrate database only (no server start)
- `-Action start` — Build, migrate, and start API (default)
- `-Action test` — Test that the running API is responding
- `-Action stop` — Stop the API server and database

**Example workflow:**

```powershell
# Start everything
.\.claude\skills\run-mobilityCenter\driver.ps1 -Action start

# In another terminal, test the API
curl http://localhost:5000/api/bicicletarios

# Stop when done
.\.claude\skills\run-mobilityCenter\driver.ps1 -Action stop
```

## Run (Human Path)

For interactive development:

```bash
# Start database
docker compose up -d

# Terminal 1: Run the API with hot reload
dotnet watch run --project ./src/MobilityCenter.API

# Terminal 2: Optional - run frontend dev server
dotnet run --project ./src/MobilityCenter.Frontend --urls http://localhost:5200
```

The API auto-reloads on file changes when using `dotnet watch run`.

## API Endpoints

Core endpoints for testing:

**Bikes Racks (Bicicletarios):**
```bash
# Get all bike racks
curl http://localhost:5000/api/bicicletarios

# Get specific bike rack
curl http://localhost:5000/api/bicicletarios/{id}
```

**Ratings (Avaliacoes):**
```bash
# Get ratings for a bike rack
curl http://localhost:5000/api/avaliacoes?bicicletarioId={id}
```

**Users (Usuarios):**
```bash
# Authentication and user endpoints
curl http://localhost:5000/api/usuarios
```

**Full API documentation:** Open `http://localhost:5000/scalar/v1` in a browser for interactive Scalar API explorer.

## Gotchas

**1. Port conflicts:** If port 5000 is already in use by another process:
   - The driver attempts to kill existing `dotnet run` processes
   - If that fails, stop the conflicting process manually: `netstat -ano | findstr :5000`

**2. Docker not running:** The script requires Docker Desktop to be running:
   - Check: `docker ps`
   - If it fails, launch Docker Desktop and wait ~30s for daemon startup

**3. Database connection timeouts:** On first startup, PostgreSQL may take 10+ seconds to be ready:
   - The driver waits for DB healthcheck (configured in `docker-compose.yml`)
   - If migrations fail, check Docker logs: `docker logs mobilitycenter_db`

**4. Slow API startup on first run:** The first `dotnet run` compiles Roslyn and warms up the runtime:
   - Expect ~15-20s on first run, ~3-5s on subsequent runs
   - The driver waits up to 15s by default; use `-Wait 30` if it times out

**5. HTTPS warnings in development:** The API redirects HTTP to HTTPS but local certs may not be trusted:
   - Use `http://localhost:5000` (not `https://localhost:7000`) for curl/Invoke-WebRequest
   - Browser access may show security warnings; ignore for development

**6. Frontend is separate:** The Blazor WASM frontend is a separate project:
   - The API hosts Scalar API docs, not the frontend
   - Frontend runs on port 5200 via: `dotnet run --project ./src/MobilityCenter.Frontend`
   - Frontend calls the API at `http://localhost:5000` (CORS configured)

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| `Cannot connect to Docker daemon` | Docker not running | Launch Docker Desktop, wait 30s |
| `error: database "mobilitycenter" does not exist` | DB creation failed | `docker compose down && docker compose up -d && dotnet ef database update ...` |
| `Address already in use (port 5000)` | Another process uses port 5000 | Kill via `netstat -ano \| findstr :5000` or use `-Action stop` |
| `API returns 404 on /api/bicicletarios` | Service not registered in DI | Check `src/MobilityCenter.API/Program.cs` for `AddBusinessServices()` |
| `Migrations fail: "Host name resolution failed"` | DNS/DB unreachable | Verify `docker ps` shows `mobilitycenter_db`, check `docker logs mobilitycenter_db` |
| `"No running servers for this workspace"` when using preview tools | Preview server timeout | The API is running via external process, not the preview tool. Use curl directly. |

## Direct Invocation (For Tests)

To test business logic without the full API:

```bash
# Run unit tests
dotnet test

# Run integration tests against real database
dotnet test --filter "IntegrationTests"
```

See `src/MobilityCenter.Business/Services/` for service classes that can be instantiated and tested directly.

## Verification

After running `driver.ps1 -Action start`, verify these endpoints:

```bash
# API is responding
curl http://localhost:5000/api/bicicletarios
# → Should return: []

# Database is ready
curl http://localhost:5000/api/usuarios
# → Should return: []

# Scalar docs are served
curl http://localhost:5000/scalar/v1
# → Should return HTML with API explorer
```

All endpoints should return HTTP 200 (or 401 if authentication is required).

---

**Last verified:** 2026-06-07 on Windows 11 with .NET 10, Docker Desktop 4.x, PowerShell 5.1
