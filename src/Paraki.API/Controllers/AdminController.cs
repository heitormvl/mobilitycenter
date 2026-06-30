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
        if (tipo != TipoUsuario.Admin)
            return Forbid();

        var sugestoes = await _db.SugestoesEdicao.CountAsync(s => s.Status == StatusSugestao.Pendente);
        var bicis = await _db.Bicicletarios
            .IgnoreQueryFilters()
            .CountAsync(b => b.StatusAprovacao == StatusBicicletario.Pendente
                          || b.StatusAprovacao == StatusBicicletario.AutoAprovado);

        return Ok(new PendenciasContagemDto { Sugestoes = sugestoes, Bicis = bicis });
    }

    private TipoUsuario ObterTipoUsuario()
    {
        var valor = User.FindFirstValue("tipo") ?? "0";
        return int.TryParse(valor, out var n) ? (TipoUsuario)n : TipoUsuario.Usuario;
    }
}
