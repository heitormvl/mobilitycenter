using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Paraki.Business.Interfaces;
using Paraki.Repositories.Context;
using Paraki.Shared.DTOs.Bicicletario;
using Paraki.Shared.Enums;
using Paraki.Shared.Models;

namespace Paraki.API.Controllers;

[ApiController]
[Route("api/fotos")]
public class FotosController : ControllerBase
{
    private readonly IFotoStorageService _fotoStorage;
    private readonly ParakiDbContext _db;

    public FotosController(IFotoStorageService fotoStorage, ParakiDbContext db)
    {
        _fotoStorage = fotoStorage;
        _db = db;
    }

    // ── Profile photo ────────────────────────────────────────────────────────

    [HttpGet("{usuarioId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> ObterFotoPerfil(Guid usuarioId)
    {
        var result = await _fotoStorage.DownloadFotoPerfilAsync(usuarioId);

        if (result is null)
            return NotFound();

        var (stream, contentType) = result.Value;
        Response.Headers.CacheControl = "public, max-age=3600";
        return File(stream, contentType);
    }

    // ── Bicicletário photos ──────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ListarFotos([FromQuery] Guid bicicletarioId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var fotos = await _db.FotosBicicletario
            .Where(f => f.BicicletarioId == bicicletarioId)
            .OrderBy(f => f.Ordem)
            .Skip(skip)
            .Take(take)
            .Select(f => new FotoBicicletarioDto
            {
                Id = f.Id,
                BicicletarioId = f.BicicletarioId,
                FotoUrl = $"/api/fotos/bicicletario/{f.BicicletarioId}/{f.Id}",
                IsCapa = f.IsCapa,
                Ordem = f.Ordem,
                CriadoEm = f.CriadoEm
            })
            .ToListAsync();

        return Ok(fotos);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UploadFotoBicicletario([FromQuery] Guid bicicletarioId, IFormFile foto)
    {
        if (foto is null || foto.Length == 0)
            return BadRequest(new { error = true, message = "Arquivo inválido." });

        if (foto.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = true, message = "Arquivo muito grande. Máximo 5MB." });

        var allowedTypes = new[] { "image/webp", "image/png", "image/jpeg", "image/heic", "image/heif" };
        if (!allowedTypes.Contains(foto.ContentType.ToLowerInvariant()))
            return BadRequest(new { error = true, message = "Tipo de arquivo não suportado. Use webp, png, jpeg ou heic." });

        var fotoId = Guid.NewGuid();

        using var stream = foto.OpenReadStream();
        await _fotoStorage.UploadFotoBicicletarioAsync(bicicletarioId, fotoId, stream, foto.ContentType);

        var maxOrdem = await _db.FotosBicicletario
            .Where(f => f.BicicletarioId == bicicletarioId)
            .Select(f => (int?)f.Ordem)
            .MaxAsync() ?? -1;

        var isFirst = maxOrdem == -1;

        var novaFoto = new FotoBicicletario
        {
            Id = fotoId,
            BicicletarioId = bicicletarioId,
            BlobKey = $"{fotoId}.webp",
            IsCapa = isFirst,
            Ordem = maxOrdem + 1,
            CriadoEm = DateTime.UtcNow
        };

        _db.FotosBicicletario.Add(novaFoto);
        await _db.SaveChangesAsync();

        return Ok(new FotoBicicletarioDto
        {
            Id = novaFoto.Id,
            BicicletarioId = novaFoto.BicicletarioId,
            FotoUrl = $"/api/fotos/bicicletario/{bicicletarioId}/{fotoId}",
            IsCapa = novaFoto.IsCapa,
            Ordem = novaFoto.Ordem,
            CriadoEm = novaFoto.CriadoEm
        });
    }

    [HttpGet("bicicletario/{bicicletarioId:guid}/{fotoId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> ObterFotoEspecifica(Guid bicicletarioId, Guid fotoId)
    {
        var result = await _fotoStorage.DownloadFotoBicicletarioAsync(bicicletarioId, fotoId);

        if (result is null)
            return NotFound();

        var (stream, contentType) = result.Value;
        Response.Headers.CacheControl = "public, max-age=3600";
        return File(stream, contentType);
    }

    [HttpGet("bicicletario/{bicicletarioId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> ObterFotoBicicletario(Guid bicicletarioId)
    {
        var capa = await _db.FotosBicicletario
            .Where(f => f.BicicletarioId == bicicletarioId && f.IsCapa)
            .FirstOrDefaultAsync();

        if (capa is not null)
        {
            var result = await _fotoStorage.DownloadFotoBicicletarioAsync(bicicletarioId, capa.Id);
            if (result is not null)
            {
                var (s, ct) = result.Value;
                Response.Headers.CacheControl = "public, max-age=3600";
                return File(s, ct);
            }
        }

        // Backward compat: serve old single-blob if no DB record exists
        var legacy = await _fotoStorage.DownloadFotoBicicletarioLegacyAsync(bicicletarioId);
        if (legacy is null)
            return NotFound();

        var (stream, contentType) = legacy.Value;
        Response.Headers.CacheControl = "public, max-age=3600";
        return File(stream, contentType);
    }

    [HttpPatch("{fotoId:guid}/capa")]
    [Authorize]
    public async Task<IActionResult> SetCapa(Guid fotoId)
    {
        if (ObterTipoUsuario() != TipoUsuario.Admin)
            return Forbid();

        var foto = await _db.FotosBicicletario.FindAsync(fotoId);
        if (foto is null)
            return NotFound(new { error = true, message = "Foto não encontrada." });

        await _db.FotosBicicletario
            .Where(f => f.BicicletarioId == foto.BicicletarioId)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.IsCapa, false));

        foto.IsCapa = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{fotoId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteFoto(Guid fotoId)
    {
        if (ObterTipoUsuario() != TipoUsuario.Admin)
            return Forbid();

        var foto = await _db.FotosBicicletario.FindAsync(fotoId);
        if (foto is null)
            return NotFound(new { error = true, message = "Foto não encontrada." });

        await _fotoStorage.DeleteFotoBicicletarioAsync(foto.BicicletarioId, foto.Id);
        _db.FotosBicicletario.Remove(foto);
        await _db.SaveChangesAsync();

        if (foto.IsCapa)
        {
            var proxima = await _db.FotosBicicletario
                .Where(f => f.BicicletarioId == foto.BicicletarioId)
                .OrderBy(f => f.Ordem)
                .FirstOrDefaultAsync();

            if (proxima is not null)
            {
                proxima.IsCapa = true;
                await _db.SaveChangesAsync();
            }
        }

        return NoContent();
    }

    private TipoUsuario ObterTipoUsuario()
    {
        var valor = User.FindFirstValue("tipo") ?? "0";
        return int.TryParse(valor, out var n) ? (TipoUsuario)n : TipoUsuario.Usuario;
    }
}
