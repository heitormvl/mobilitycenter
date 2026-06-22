using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paraki.Business.Interfaces;
using Paraki.Shared.DTOs.SugestaoEdicao;
using Paraki.Shared.Exceptions;

namespace Paraki.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class SugestoesController : ControllerBase
{
    private readonly ISugestaoEdicaoService _sugestoesService;
    private readonly IBicicletarioService _bicicletarioService;

    public SugestoesController(ISugestaoEdicaoService sugestoesService, IBicicletarioService bicicletarioService)
    {
        _sugestoesService = sugestoesService;
        _bicicletarioService = bicicletarioService;
    }

    [HttpGet("bicicletarios/{bicicletarioId:guid}/sugestoes")]
    public async Task<IActionResult> Listar(Guid bicicletarioId)
    {
        var usuarioId = ObterUsuarioId();
        var resultado = await _sugestoesService.ListarPorBicicletarioAsync(bicicletarioId, usuarioId);
        return Ok(resultado);
    }

    [HttpPost("sugestoes/{sugestaoId:guid}/aprovar")]
    public async Task<IActionResult> Aprovar(Guid sugestaoId)
    {
        var usuarioId = ObterUsuarioId();
        var bicicletario = await _sugestoesService.AprovarAsync(sugestaoId, usuarioId);
        return Ok(bicicletario);
    }

    [HttpPost("sugestoes/{sugestaoId:guid}/rejeitar")]
    public async Task<IActionResult> Rejeitar(Guid sugestaoId, [FromBody] AvaliarSugestaoDto dto)
    {
        var usuarioId = ObterUsuarioId();
        var sugestao = await _sugestoesService.RejeitarAsync(sugestaoId, usuarioId, dto.Motivo);
        return Ok(sugestao);
    }

    private Guid ObterUsuarioId()
    {
        var valor = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AppException("Usuário não autenticado.", 401);
        return Guid.Parse(valor);
    }
}
