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
}
