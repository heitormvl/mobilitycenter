using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobilityCenter.Business.Interfaces;
using MobilityCenter.Shared.DTOs.Usuario;
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

    [HttpPut("me")]
    public async Task<IActionResult> AtualizarPerfil([FromBody] AtualizarPerfilDto dto)
    {
        var usuarioId = ObterUsuarioId();
        var resultado = await _usuarioService.AtualizarPerfilAsync(usuarioId, dto);
        return Ok(resultado);
    }

    [HttpPut("me/senha")]
    public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaDto dto)
    {
        var usuarioId = ObterUsuarioId();
        await _usuarioService.AlterarSenhaAsync(usuarioId, dto);
        return NoContent();
    }

    [HttpPost("me/foto")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> AtualizarFoto(IFormFile foto)
    {
        if (foto is null || foto.Length == 0)
            return BadRequest(new { error = true, message = "Nenhuma foto enviada." });

        var tiposPermitidos = new[] { "image/webp", "image/png", "image/jpeg", "image/heic", "image/heif" };
        if (!tiposPermitidos.Contains(foto.ContentType.ToLowerInvariant()))
            return BadRequest(new { error = true, message = "Formato inválido. Use WEBP, PNG, JPG ou HEIC." });

        var usuarioId = ObterUsuarioId();
        using var stream = foto.OpenReadStream();
        var url = await _usuarioService.AtualizarFotoPerfilAsync(usuarioId, stream, foto.ContentType);

        return Ok(new { url });
    }

    [HttpDelete("me")]
    public async Task<IActionResult> ExcluirConta()
    {
        var usuarioId = ObterUsuarioId();
        await _usuarioService.ExcluirContaAsync(usuarioId);
        return NoContent();
    }

    private Guid ObterUsuarioId()
    {
        var valor = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AppException("Usuário não autenticado.", 401);
        return Guid.Parse(valor);
    }
}
