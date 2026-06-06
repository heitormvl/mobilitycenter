using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobilityCenter.Business.Interfaces;
using MobilityCenter.Shared.Exceptions;

namespace MobilityCenter.API.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService) => _usuarioService = usuarioService;

    [HttpGet("me")]
    public async Task<IActionResult> ObterPerfil()
    {
        var usuarioId = ObterUsuarioId();
        var resultado = await _usuarioService.ObterPerfilAsync(usuarioId);
        return Ok(resultado);
    }

    [HttpGet("me/avaliacoes")]
    public async Task<IActionResult> ObterAvaliacoes()
    {
        var usuarioId = ObterUsuarioId();
        var resultado = await _usuarioService.ObterAvaliacoesAsync(usuarioId);
        return Ok(resultado);
    }

    [HttpGet("me/bicicletarios")]
    public async Task<IActionResult> ObterBicicletarios()
    {
        var usuarioId = ObterUsuarioId();
        var resultado = await _usuarioService.ObterBicicletariosAsync(usuarioId);
        return Ok(resultado);
    }

    private Guid ObterUsuarioId()
    {
        var valor = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AppException("Usuário não autenticado.", 401);
        return Guid.Parse(valor);
    }
}
