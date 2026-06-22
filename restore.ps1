# restore.ps1 -- Restaura banco + imagens a partir de um .zip gerado pelo backup.ps1
# Pre-requisito: container paraki_db rodando (docker compose up db)
#
# Uso:
#   .\restore.ps1 -Zip .\paraki_backup_20260622_1430.zip

param(
    [Parameter(Mandatory)]
    [string]$Zip
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $Zip)) { throw "Arquivo nao encontrado: $Zip" }

$tempDir = Join-Path $env:TEMP "paraki_restore_$(Get-Random)"
Expand-Archive -Path $Zip -DestinationPath $tempDir

try {
    # 1. Restaurar banco
    $dumpFile = "$tempDir\db.dump"
    if (-not (Test-Path $dumpFile)) { throw "db.dump nao encontrado no zip." }

    Write-Host "-> Restaurando banco de dados..."
    docker cp "$dumpFile" "paraki_db:/tmp/mc_restore.dump"
    if ($LASTEXITCODE -ne 0) { throw "docker cp falhou (exit $LASTEXITCODE)" }

    # --clean descarta objetos existentes antes de recriar; --if-exists evita erro se o schema esta vazio
    docker exec paraki_db pg_restore -U mc_user -d paraki --clean --if-exists --no-owner /tmp/mc_restore.dump
    docker exec paraki_db rm /tmp/mc_restore.dump | Out-Null

    # 2. Restaurar imagens
    $srcImages = "$tempDir\fotos-perfil"
    if (Test-Path $srcImages) {
        $destImages = Join-Path $env:TEMP "paraki\fotos-perfil"
        Write-Host "-> Restaurando imagens para $destImages ..."
        if (Test-Path $destImages) {
            Remove-Item -Path $destImages -Recurse -Force
        }
        New-Item -ItemType Directory -Path (Split-Path $destImages) -Force | Out-Null
        Copy-Item -Path $srcImages -Destination $destImages -Recurse
    } else {
        Write-Host "   (nenhuma imagem no backup -- pulando)"
    }

    Write-Host ""
    Write-Host "Restore concluido. Reinicie a API se ja estava rodando."
}
finally {
    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
