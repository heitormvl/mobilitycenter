using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobilityCenter.Business.Filters;
using MobilityCenter.Business.Interfaces;
using MobilityCenter.Shared.DTOs.Bicicletario;
using MobilityCenter.Shared.Exceptions;

namespace MobilityCenter.API.Controllers;

[ApiController]
[Route("api/bicicletarios")]
public class BicicletariosController : ControllerBase
{
    private readonly IBicicletarioService _bicicletarioService;

    public BicicletariosController(IBicicletarioService bicicletarioService)
        => _bicicletarioService = bicicletarioService;

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] BicicletarioFiltros filtros)
    {
        var resultado = await _bicicletarioService.ListarAsync(filtros);
        return Ok(resultado);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var resultado = await _bicicletarioService.ObterPorIdAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Criar([FromBody] CriarBicicletarioDto dto)
    {
        var usuarioId = ObterUsuarioId();
        var resultado = await _bicicletarioService.CriarAsync(dto, usuarioId);
        return Created($"/api/bicicletarios/{resultado.Id}", resultado);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarBicicletarioDto dto)
    {
        var usuarioId = ObterUsuarioId();
        var resultado = await _bicicletarioService.AtualizarAsync(id, dto, usuarioId);
        return Ok(resultado);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Deletar(Guid id)
    {
        var usuarioId = ObterUsuarioId();
        await _bicicletarioService.DeletarAsync(id, usuarioId);
        return NoContent();
    }

    private Guid ObterUsuarioId()
    {
        var valor = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AppException("Usuário não autenticado.", 401);
        return Guid.Parse(valor);
    }
}
