<#
.SYNOPSIS
Deploy local da MobilityCenter.
Sobe: banco + API + Frontend e expoe ambos via Tailscale.

.EXAMPLE
./deploy-local.ps1          # sobe tudo (reutiliza imagens existentes)
./deploy-local.ps1 -Rebuild # forca rebuild das imagens antes de subir
#>

param(
    [switch]$Rebuild
)

$ErrorActionPreference = "Stop"

function Write-Ok   { param([string]$Msg) Write-Host "[OK] $Msg" -ForegroundColor Green }
function Write-Info { param([string]$Msg) Write-Host "[..] $Msg" -ForegroundColor Cyan }
function Write-Warn { param([string]$Msg) Write-Host "[!!] $Msg" -ForegroundColor Yellow }
function Write-Fail { param([string]$Msg) Write-Host "[XX] $Msg" -ForegroundColor Red; exit 1 }

function Test-Cmd {
    param([string]$Name)
    return ($null -ne (Get-Command $Name -ErrorAction SilentlyContinue))
}

# ===== PRE-REQUISITOS =====
Write-Info "Verificando pre-requisitos..."

if (-not (Test-Cmd docker)) {
    Write-Fail "Docker nao encontrado. Instale em: https://www.docker.com/products/docker-desktop"
}
Write-Ok "Docker instalado"

Write-Info "Verificando se Docker daemon esta rodando..."
docker info 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[XX] Docker Desktop nao esta rodando." -ForegroundColor Red
    Write-Host ""
    Write-Host "  1. Abra o Docker Desktop" -ForegroundColor Yellow
    Write-Host "  2. Aguarde o icone na bandeja ficar estavel" -ForegroundColor Yellow
    Write-Host "  3. Execute este script novamente" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
Write-Ok "Docker daemon ativo"

if (-not (Test-Cmd docker-compose)) {
    Write-Fail "docker-compose nao encontrado"
}
Write-Ok "docker-compose instalado"

if (-not (Test-Path "docker-compose.yml")) {
    Write-Fail "docker-compose.yml nao encontrado na raiz do projeto"
}
Write-Ok "docker-compose.yml encontrado"

# ===== TAILSCALE =====
$tsHostname = $null
$apiFrontendOrigin = "http://localhost:5001"
$apiBaseUrl        = "http://localhost:5000"

if (Test-Cmd tailscale) {
    Write-Info "Obtendo hostname do Tailscale..."
    try {
        $tsJson    = (& tailscale status --json 2>&1) | ConvertFrom-Json
        $tsRaw     = $tsJson.Self.DNSName -replace '\.$', ''
        if ($tsRaw) {
            $tsHostname        = $tsRaw
            $apiFrontendOrigin = "https://$tsHostname"
            $apiBaseUrl        = "https://$tsHostname`:5000"
            Write-Ok "Tailscale: $tsHostname"
        }
    } catch {
        Write-Warn "Tailscale instalado mas nao conectado. Usando localhost."
    }
} else {
    Write-Warn "Tailscale nao instalado. Usando localhost."
}

# ===== ATUALIZAR appsettings.json DO FRONTEND =====
Write-Info "Atualizando ApiBaseUrl do frontend para: $apiBaseUrl"
$appSettingsPath = "src/MobilityCenter.Frontend/wwwroot/appsettings.json"
$appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
$appSettings.ApiBaseUrl = $apiBaseUrl
$appSettings | ConvertTo-Json -Depth 5 | Set-Content $appSettingsPath -Encoding utf8
Write-Ok "appsettings.json atualizado"

# ===== GERAR .env PARA CORS =====
$envContent = "CORS_ORIGIN_0=http://localhost:5001`nCORS_ORIGIN_1=$apiFrontendOrigin"
Set-Content -Path ".env" -Value $envContent -Encoding utf8
Write-Ok ".env gerado (CORS configurado)"

# ===== PARAR CONTAINERS ANTERIORES =====
Write-Info "Parando containers anteriores..."
$ErrorActionPreference = "Continue"
docker-compose down --remove-orphans
$ErrorActionPreference = "Stop"
Write-Ok "Containers parados"

# ===== REBUILD (opcional) =====
if ($Rebuild) {
    Write-Info "Reconstruindo imagens..."
    $ErrorActionPreference = "Continue"
    docker-compose build --no-cache
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = "Stop"
    if ($exitCode -ne 0) { Write-Fail "Erro ao buildar imagens" }
    Write-Ok "Imagens reconstruidas"
}

# ===== SUBIR CONTAINERS =====
Write-Info "Iniciando containers (db + api + frontend)..."
$ErrorActionPreference = "Continue"
docker-compose up -d
$exitCode = $LASTEXITCODE
$ErrorActionPreference = "Stop"
if ($exitCode -ne 0) { Write-Fail "Erro ao iniciar docker-compose" }
Write-Ok "Containers iniciados em background"

# ===== AGUARDAR API =====
Write-Info "Aguardando API ficar saudavel..."
$maxAttempts = 60
$attempt     = 0
$healthy     = $false

while ($attempt -lt $maxAttempts -and (-not $healthy)) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -TimeoutSec 2 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            $healthy = $true
            Write-Ok "API saudavel"
        }
    } catch {
        $attempt++
        if (($attempt % 10) -eq 0) {
            Write-Host "  Tentativa $attempt/$maxAttempts..." -ForegroundColor Gray
        }
        Start-Sleep -Seconds 1
    }
}

if (-not $healthy) {
    Write-Host ""
    Write-Warn "API nao respondeu apos $maxAttempts tentativas."
    Write-Host "  Veja os logs da API: docker-compose logs api" -ForegroundColor Gray
    Write-Host "  Veja os logs do DB:  docker-compose logs db" -ForegroundColor Gray
    Write-Host ""
    exit 1
}

# ===== TAILSCALE SERVE =====
if ($tsHostname) {
    Write-Info "Configurando Tailscale Serve..."
    $ErrorActionPreference = "Continue"

    # Frontend: https://hostname/ -> localhost:5001
    $out = & tailscale serve --bg http://localhost:5001 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Warn "serve frontend: $out" }

    # API: https://hostname:5000/ -> localhost:5000
    $out = & tailscale serve --bg --https 5000 http://localhost:5000 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Warn "serve api: $out" }

    $ErrorActionPreference = "Stop"

    # Mostrar status final
    Write-Host ""
    & tailscale serve status
    Write-Host ""
    Write-Ok "Tailscale Serve configurado"
}

# ===== RESUMO =====
Write-Host ""
Write-Host "=======================================" -ForegroundColor Green
Write-Host "  DEPLOY LOCAL CONCLUIDO" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Green
Write-Host ""

if ($tsHostname) {
    Write-Host "  Tailscale (compartilhavel):" -ForegroundColor Cyan
    Write-Host "    Frontend: https://$tsHostname" -ForegroundColor Green
    Write-Host "    API:      https://$tsHostname`:5000" -ForegroundColor Green
    Write-Host ""
}

Write-Host "  Local:" -ForegroundColor Cyan
Write-Host "    Frontend:    http://localhost:5001" -ForegroundColor White
Write-Host "    API:         http://localhost:5000" -ForegroundColor White
Write-Host "    Scalar docs: http://localhost:5000/scalar/v1" -ForegroundColor White
Write-Host "    PostgreSQL:  localhost:5432" -ForegroundColor White
Write-Host ""
Write-Host "  Logs:        docker-compose logs -f" -ForegroundColor Gray
Write-Host "  Parar:       docker-compose down" -ForegroundColor Gray
Write-Host "  Limpar DB:   docker-compose down -v" -ForegroundColor Gray
Write-Host "  Rebuild:     ./deploy-local.ps1 -Rebuild" -ForegroundColor Gray
Write-Host ""
