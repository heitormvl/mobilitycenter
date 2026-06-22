using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paraki.Business.Filters;
using Paraki.Business.Interfaces;
using Paraki.Shared.DTOs.Bicicletario;
using Paraki.Shared.Enums;
using Paraki.Shared.Exceptions;

namespace Paraki.API.Controllers;

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
        filtros.IncluirOcultas = false;
        var resultado = await _bicicletarioService.ListarAsync(filtros);
        return Ok(resultado);
    }

    [HttpGet("admin")]
    [Authorize]
    public async Task<IActionResult> ListarAdmin([FromQuery] BicicletarioFiltros filtros)
    {
        var tipo = ObterTipoUsuario();
        if (tipo != TipoUsuario.Admin)
            return Forbid();

        filtros.IncluirOcultas = true;
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

        if (resultado.EditadoDireto)
            return Ok(resultado.Bicicletario);

        return Accepted(resultado.Sugestao);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Deletar(Guid id)
    {
        var usuarioId = ObterUsuarioId();
        var tipo = ObterTipoUsuario();
        await _bicicletarioService.DeletarAsync(id, usuarioId, tipo);
        return NoContent();
    }

    [HttpPatch("{id:guid}/restaurar")]
    [Authorize]
    public async Task<IActionResult> Restaurar(Guid id)
    {
        var tipo = ObterTipoUsuario();
        await _bicicletarioService.RestaurarAsync(id, tipo);
        return NoContent();
    }

    [HttpDelete("{id:guid}/permanente")]
    [Authorize]
    public async Task<IActionResult> DeletarPermanente(Guid id)
    {
        var usuarioId = ObterUsuarioId();
        var tipo = ObterTipoUsuario();
        await _bicicletarioService.DeletarPermanenteAsync(id, usuarioId, tipo);
        return NoContent();
    }

    private Guid ObterUsuarioId()
    {
        var valor = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AppException("Usuário não autenticado.", 401);
        return Guid.Parse(valor);
    }

    private TipoUsuario ObterTipoUsuario()
    {
        var valor = User.FindFirstValue("tipo") ?? "0";
        return int.TryParse(valor, out var n) ? (TipoUsuario)n : TipoUsuario.Usuario;
    }

    private TipoUsuario? ObterTipoUsuarioOuNulo()
    {
        var valor = User.FindFirstValue("tipo");
        if (valor == null) return null;
        return int.TryParse(valor, out var n) ? (TipoUsuario)n : null;
    }
}
