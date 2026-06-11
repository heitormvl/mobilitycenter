using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobilityCenter.Business.Interfaces;

namespace MobilityCenter.API.Controllers;

[ApiController]
[Route("api/fotos")]
public class FotosController : ControllerBase
{
    private readonly IFotoStorageService _fotoStorage;

    public FotosController(IFotoStorageService fotoStorage) => _fotoStorage = fotoStorage;

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

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UploadFotoBicicletario([FromQuery] Guid bicicletarioId, IFormFile foto)
    {
        if (foto is null || foto.Length == 0)
            return BadRequest(new { error = true, message = "Arquivo inválido." });

        if (foto.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = true, message = "Arquivo muito grande. Máximo 5MB." });

        var allowedTypes = new[] { "image/webp", "image/png", "image/jpeg" };
        if (!allowedTypes.Contains(foto.ContentType))
            return BadRequest(new { error = true, message = "Tipo de arquivo não suportado. Use webp, png ou jpeg." });

        using var stream = foto.OpenReadStream();
        var url = await _fotoStorage.UploadFotoBicicletarioAsync(bicicletarioId, stream, foto.ContentType);
        return Ok(new { url });
    }

    [HttpGet("bicicletario/{bicicletarioId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> ObterFotoBicicletario(Guid bicicletarioId)
    {
        var result = await _fotoStorage.DownloadFotoBicicletarioAsync(bicicletarioId);

        if (result is null)
            return NotFound();

        var (stream, contentType) = result.Value;
        Response.Headers.CacheControl = "public, max-age=3600";
        return File(stream, contentType);
    }
}
