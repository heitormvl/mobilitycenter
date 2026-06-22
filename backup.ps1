# backup.ps1 — Gera um .zip com dump do banco + imagens para transportar entre máquinas.
# Pré-requisito: container mobilitycenter_db rodando (docker compose up db)
#
# Uso:
#   .\backup.ps1
#   .\backup.ps1 -Out "C:\backups\meu_backup.zip"

param(
    [string]$Out = ""
)

$ErrorActionPreference = "Stop"

$timestamp = Get-Date -Format "yyyyMMdd_HHmm"
if (-not $Out) { $Out = ".\mobilitycenter_backup_$timestamp.zip" }

$tempDir = Join-Path $env:TEMP "mobilitycenter_backup_$timestamp"
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    # ── 1. Dump do banco ──────────────────────────────────────────────────────
    Write-Host "→ Exportando banco de dados..."
    docker exec mobilitycenter_db pg_dump -U mc_user -d mobilitycenter --format=custom --file=/tmp/mc_backup.dump
    if ($LASTEXITCODE -ne 0) { throw "pg_dump falhou (exit $LASTEXITCODE)" }

    docker cp "mobilitycenter_db:/tmp/mc_backup.dump" "$tempDir\db.dump"
    if ($LASTEXITCODE -ne 0) { throw "docker cp falhou (exit $LASTEXITCODE)" }

    docker exec mobilitycenter_db rm /tmp/mc_backup.dump | Out-Null

    # ── 2. Imagens locais ─────────────────────────────────────────────────────
    $imagesPath = Join-Path $env:TEMP "mobilitycenter\fotos-perfil"
    if (Test-Path $imagesPath) {
        Write-Host "→ Copiando imagens de $imagesPath ..."
        Copy-Item -Path $imagesPath -Destination "$tempDir\fotos-perfil" -Recurse
    } else {
        Write-Host "  (nenhuma imagem local encontrada em $imagesPath — pulando)"
    }

    # ── 3. Metadados (versão, data) ───────────────────────────────────────────
    @{
        created  = (Get-Date -Format "o")
        db       = "mobilitycenter"
        db_user  = "mc_user"
        images   = if (Test-Path $imagesPath) { "fotos-perfil/" } else { $null }
    } | ConvertTo-Json | Set-Content "$tempDir\manifest.json" -Encoding utf8

    # ── 4. Compactar ──────────────────────────────────────────────────────────
    Write-Host "→ Compactando..."
    Compress-Archive -Path "$tempDir\*" -DestinationPath $Out -Force

    $sizeMb = [math]::Round((Get-Item $Out).Length / 1MB, 2)
    Write-Host ""
    Write-Host "Backup concluído: $Out ($sizeMb MB)"
    Write-Host "Para restaurar:   .\restore.ps1 -Zip '$Out'"
}
finally {
    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
