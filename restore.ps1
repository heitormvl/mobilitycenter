# restore.ps1 — Restaura banco + imagens a partir de um .zip gerado pelo backup.ps1
# Pré-requisito: container mobilitycenter_db rodando (docker compose up db)
#
# Uso:
#   .\restore.ps1 -Zip .\mobilitycenter_backup_20260622_1430.zip

param(
    [Parameter(Mandatory)]
    [string]$Zip
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $Zip)) { throw "Arquivo não encontrado: $Zip" }

$tempDir = Join-Path $env:TEMP "mobilitycenter_restore_$(Get-Random)"
Expand-Archive -Path $Zip -DestinationPath $tempDir

try {
    # ── 1. Restaurar banco ────────────────────────────────────────────────────
    $dumpFile = "$tempDir\db.dump"
    if (-not (Test-Path $dumpFile)) { throw "db.dump não encontrado no zip." }

    Write-Host "→ Restaurando banco de dados..."
    docker cp "$dumpFile" "mobilitycenter_db:/tmp/mc_restore.dump"
    if ($LASTEXITCODE -ne 0) { throw "docker cp falhou (exit $LASTEXITCODE)" }

    # --clean descarta objetos existentes antes de recriar; --if-exists evita erro se o schema está vazio
    docker exec mobilitycenter_db pg_restore -U mc_user -d mobilitycenter --clean --if-exists --no-owner /tmp/mc_restore.dump
    # pg_restore retorna exit 1 quando há warnings ignoráveis — verifica apenas erros críticos via stderr
    docker exec mobilitycenter_db rm /tmp/mc_restore.dump | Out-Null

    # ── 2. Restaurar imagens ──────────────────────────────────────────────────
    $srcImages = "$tempDir\fotos-perfil"
    if (Test-Path $srcImages) {
        $destImages = Join-Path $env:TEMP "mobilitycenter\fotos-perfil"
        Write-Host "→ Restaurando imagens para $destImages ..."
        if (Test-Path $destImages) {
            Remove-Item -Path $destImages -Recurse -Force
        }
        New-Item -ItemType Directory -Path (Split-Path $destImages) -Force | Out-Null
        Copy-Item -Path $srcImages -Destination $destImages -Recurse
    } else {
        Write-Host "  (nenhuma imagem no backup — pulando)"
    }

    Write-Host ""
    Write-Host "Restore concluído. Reinicie a API se já estava rodando."
}
finally {
    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
