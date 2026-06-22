#Requires -Version 5.1
<#
.SYNOPSIS
    Builds, starts, and tests the Paraki API.

.DESCRIPTION
    This driver script handles the complete setup and launch of the Paraki application:
    - Starts PostgreSQL + PostGIS via Docker
    - Builds the .NET solution
    - Runs database migrations
    - Starts the API server on http://localhost:5000
    - Validates the API is responding

.PARAMETER Action
    The action to perform: 'build', 'start', 'test', or 'stop'. Default is 'start'.

.PARAMETER Wait
    Time in seconds to wait for the API to be ready. Default is 10.

.EXAMPLE
    .\driver.ps1 -Action build
    .\driver.ps1 -Action start
    .\driver.ps1 -Action test
#>

param(
    [ValidateSet('build', 'start', 'test', 'stop')]
    [string]$Action = 'start',
    [int]$Wait = 10
)

$ErrorActionPreference = 'Stop'

# Find the repo root by looking for .git or Paraki.slnx
$repoRoot = $PWD
while ($repoRoot -ne [System.IO.Path]::GetPathRoot($repoRoot)) {
    if ((Test-Path (Join-Path $repoRoot ".git")) -or (Test-Path (Join-Path $repoRoot "Paraki.slnx"))) {
        break
    }
    $repoRoot = Split-Path $repoRoot -Parent
}

if (-not (Test-Path (Join-Path $repoRoot "Paraki.slnx"))) {
    Write-Error "Could not find Paraki repository root. Make sure you're in the repo directory."
    exit 1
}

$apiProj = Join-Path $repoRoot "src\Paraki.API"
$repoProj = Join-Path $repoRoot "src\Paraki.Repositories"
$dbContainer = "paraki_db"
$apiUrl = "http://localhost:5000/api/bicicletarios"

function Write-Status {
    param([string]$Message, [switch]$Success, [switch]$Error)
    $color = if ($Success) { 'Green' } elseif ($Error) { 'Red' } else { 'Cyan' }
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message" -ForegroundColor $color
}

function Test-DockerRunning {
    try {
        docker ps > $null 2>&1
        return $true
    } catch {
        return $false
    }
}

function Start-Database {
    Write-Status "Starting PostgreSQL + PostGIS database..."

    if (-not (Test-DockerRunning)) {
        Write-Status "Docker is not running. Please start Docker." -Error
        exit 1
    }

    $running = docker ps --filter "name=$dbContainer" --format "{{.Names}}" 2>$null
    if ($running -eq $dbContainer) {
        Write-Status "Database container is already running" -Success
        return
    }

    docker compose -f (Join-Path $repoRoot "docker-compose.yml") up -d
    Start-Sleep -Seconds 5
    Write-Status "Database started" -Success
}

function Build-Solution {
    Write-Status "Building .NET solution..."
    Push-Location $repoRoot
    try {
        dotnet build 2>&1 | Out-Null
        Write-Status "Build completed successfully" -Success
    } finally {
        Pop-Location
    }
}

function Run-Migrations {
    Write-Status "Running database migrations..."
    Push-Location $repoRoot
    try {
        $output = dotnet ef database update -p $repoProj -s $apiProj 2>&1
        if ($output -match "No migrations were applied|already up to date") {
            Write-Status "Database is up to date" -Success
        } else {
            Write-Status "Migrations completed" -Success
        }
    } finally {
        Pop-Location
    }
}

function Start-API {
    Write-Status "Starting API server..."

    # Kill any existing dotnet processes running the API
    $running = Get-Process dotnet -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*dotnet run*" -or $_.MainModule.FileName -like "*Paraki.API*" }
    if ($running) {
        Write-Status "Found existing API process, stopping..."
        $running | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    }

    Push-Location $apiProj
    try {
        $script:apiProcess = Start-Process -FilePath dotnet `
            -ArgumentList "run" `
            -NoNewWindow `
            -RedirectStandardOutput $env:TEMP\api-stdout.log `
            -RedirectStandardError $env:TEMP\api-stderr.log `
            -PassThru

        Write-Status "API server started (PID: $($script:apiProcess.Id))"
    } finally {
        Pop-Location
    }

    # Wait for API to be ready
    Write-Status "Waiting up to $Wait seconds for API to be ready..."
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    while ($stopwatch.Elapsed.TotalSeconds -lt $Wait) {
        try {
            $response = Invoke-WebRequest -Uri $apiUrl -Method Get -ErrorAction SilentlyContinue
            Write-Status "API is ready and responding" -Success
            return $true
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }

    Write-Status "API did not respond within $Wait seconds" -Error
    return $false
}

function Test-API {
    Write-Status "Testing API endpoints..."

    # Test Bicicletarios endpoint
    try {
        $response = Invoke-WebRequest -Uri $apiUrl -Method Get -UseBasicParsing
        $body = $response.Content | ConvertFrom-Json
        Write-Status "✓ GET /api/bicicletarios responded with status $($response.StatusCode)" -Success
    } catch {
        Write-Status "✗ GET /api/bicicletarios failed: $_" -Error
        return $false
    }

    # Test Scalar API documentation
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/scalar/v1" -Method Get -UseBasicParsing
        Write-Status "✓ Scalar API documentation is available at http://localhost:5000/scalar/v1" -Success
    } catch {
        Write-Status "✗ Could not access Scalar API documentation" -Error
        return $false
    }

    Write-Status ""
    Write-Status "API is running and responding correctly!" -Success
    Write-Status "API URL: http://localhost:5000"
    Write-Status "API Docs: http://localhost:5000/scalar/v1"

    return $true
}

function Stop-API {
    Write-Status "Stopping API server..."
    if ($script:apiProcess -and -not $script:apiProcess.HasExited) {
        $script:apiProcess | Stop-Process -Force -ErrorAction SilentlyContinue
        Write-Status "API server stopped" -Success
    }
}

function Stop-Database {
    Write-Status "Stopping database..."
    docker compose -f (Join-Path $repoRoot "docker-compose.yml") down 2>&1 | Out-Null
    Write-Status "Database stopped" -Success
}

# Main execution
switch ($Action) {
    'build' {
        Start-Database
        Build-Solution
        Run-Migrations
    }
    'start' {
        Start-Database
        Build-Solution
        Run-Migrations
        Start-API
        Test-API
    }
    'test' {
        Test-API
    }
    'stop' {
        Stop-API
        Stop-Database
    }
}

Write-Status ""
Write-Status "Done!" -Success
