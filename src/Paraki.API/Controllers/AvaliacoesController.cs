using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paraki.Business.Interfaces;
using Paraki.Shared.DTOs.Avaliacao;
using Paraki.Shared.Exceptions;

namespace Paraki.API.Controllers;

[ApiController]
[Route("api/bicicletarios/{bicicletarioId:guid}/avaliacoes")]
public class AvaliacoesController : ControllerBase
{
    private readonly IAvaliacaoService _avaliacaoService;

    public AvaliacoesController(IAvaliacaoService avaliacaoService)
        => _avaliacaoService = avaliacaoService;

    [HttpGet]
    public async Task<IActionResult> Listar(Guid bicicletarioId)
    {
        var resultado = await _avaliacaoService.ListarPorBicicletarioAsync(bicicletarioId);
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Criar(Guid bicicletarioId, [FromBody] CriarAvaliacaoDto dto)
    {
        var usuarioId = ObterUsuarioId();
        var resultado = await _avaliacaoService.CriarAsync(bicicletarioId, dto, usuarioId);
        return Created($"/api/bicicletarios/{bicicletarioId}/avaliacoes/{resultado.Id}", resultado);
    }

    private Guid ObterUsuarioId()
    {
        var valor = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AppException("Usuário não autenticado.", 401);
        return Guid.Parse(valor);
    }
}
