using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Paraki.Repositories.Context;
using Paraki.Shared.DTOs.Admin;
using Paraki.Shared.Enums;
using Paraki.Shared.Exceptions;

namespace Paraki.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly ParakiDbContext _db;

    public AdminController(ParakiDbContext db) => _db = db;

    [HttpGet("pendencias/contagem")]
    public async Task<IActionResult> ContarPendencias()
    {
        var tipo = ObterTipoUsuario();
        if (tipo != TipoUsuario.Admin && tipo != TipoUsuario.Moderador)
            return Forbid();

        var sugestoes = await _db.SugestoesEdicao.CountAsync(s => s.Status == StatusSugestao.Pendente);
        var bicis = await _db.Bicicletarios
            .IgnoreQueryFilters()
            .CountAsync(b => b.StatusAprovacao == StatusBicicletario.Pendente
                          || b.StatusAprovacao == StatusBicicletario.AutoAprovado);

        return Ok(new PendenciasContagemDto { Sugestoes = sugestoes, Bicis = bicis });
    }

    [HttpGet("usuarios")]
    public async Task<IActionResult> ListarUsuarios([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var tipo = ObterTipoUsuario();
        if (tipo != TipoUsuario.Admin)
            return Forbid();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await _db.Users.CountAsync();
        var usuarios = await _db.Users
            .OrderBy(u => u.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UsuarioAdminDto
            {
                Id = u.Id,
                Nome = u.DisplayName,
                Email = u.Email ?? "",
                Tipo = (int)u.Type,
                TierEfetivo = (int)(u.TierOverride ?? (u.PontosAprovados >= 50 ? TipoTier.Ouro : u.PontosAprovados >= 10 ? TipoTier.Prata : TipoTier.Padrao)),
                TierOverride = u.TierOverride.HasValue ? (int?)u.TierOverride.Value : null,
                PontosAprovados = u.PontosAprovados,
                CriadoEm = u.CreatedAt,
            })
            .ToListAsync();

        return Ok(new UsuariosPageDto { Usuarios = usuarios, Total = total, Page = page, PageSize = pageSize });
    }

    [HttpPut("usuarios/{id:guid}/role")]
    public async Task<IActionResult> AlterarRole(Guid id, [FromBody] AlterarRoleDto dto)
    {
        var tipo = ObterTipoUsuario();
        if (tipo != TipoUsuario.Admin)
            return Forbid();

        var usuario = await _db.Users.FindAsync(id);
        if (usuario == null) return NotFound(new { error = true, message = "Usuário não encontrado." });

        var novoTipo = (TipoUsuario)dto.Role;
        if (!Enum.IsDefined(novoTipo))
            return BadRequest(new { error = true, message = "Role inválido." });

        usuario.Type = novoTipo;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("usuarios/{id:guid}/tier")]
    public async Task<IActionResult> AlterarTier(Guid id, [FromBody] AlterarTierDto dto)
    {
        var tipo = ObterTipoUsuario();
        if (tipo != TipoUsuario.Admin)
            return Forbid();

        var usuario = await _db.Users.FindAsync(id);
        if (usuario == null) return NotFound(new { error = true, message = "Usuário não encontrado." });

        if (dto.Tier.HasValue && !Enum.IsDefined((TipoTier)dto.Tier.Value))
            return BadRequest(new { error = true, message = "Tier inválido." });

        usuario.TierOverride = dto.Tier.HasValue ? (TipoTier?)dto.Tier.Value : null;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private TipoUsuario ObterTipoUsuario()
    {
        var valor = User.FindFirstValue("tipo") ?? "0";
        return int.TryParse(valor, out var n) ? (TipoUsuario)n : TipoUsuario.Usuario;
    }
}
