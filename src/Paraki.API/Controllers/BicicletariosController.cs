using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Paraki.Business.Filters;
using Paraki.Business.Interfaces;
using Paraki.Shared.DTOs.Bicicletario;
using Paraki.Shared.DTOs.SugestaoEdicao;
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
        var adminId = ObterUsuarioId();
        var tipo = ObterTipoUsuario();
        await _bicicletarioService.RestaurarAsync(id, adminId, tipo);
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

    [HttpGet("pendentes")]
    [Authorize]
    public async Task<IActionResult> ListarPendentes()
    {
        var adminId = ObterUsuarioId();
        var resultado = await _bicicletarioService.ListarPendentesAsync(adminId);
        return Ok(resultado);
    }

    [HttpPost("{id:guid}/aprovar")]
    [Authorize]
    public async Task<IActionResult> AprovarCriacao(Guid id)
    {
        var adminId = ObterUsuarioId();
        var resultado = await _bicicletarioService.AprovarCriacaoAsync(id, adminId);
        return Ok(resultado);
    }

    [HttpPost("{id:guid}/rejeitar")]
    [Authorize]
    public async Task<IActionResult> RejeitarCriacao(Guid id, [FromBody] AvaliarSugestaoDto dto)
    {
        var adminId = ObterUsuarioId();
        await _bicicletarioService.RejeitarCriacaoAsync(id, adminId, dto.Motivo);
        return NoContent();
    }

    [HttpGet("{id:guid}/auditoria")]
    [Authorize]
    public async Task<IActionResult> ObterAuditoria(Guid id)
    {
        var adminId = ObterUsuarioId();
        var resultado = await _bicicletarioService.ObterAuditoriaAsync(id, adminId);
        return Ok(resultado);
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
