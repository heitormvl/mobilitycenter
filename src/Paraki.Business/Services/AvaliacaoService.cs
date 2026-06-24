using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Paraki.Business.Interfaces;
using Paraki.Repositories.Context;
using Paraki.Shared.DTOs.Avaliacao;
using Paraki.Shared.Exceptions;
using Paraki.Shared.Models;

namespace Paraki.Business.Services;

public class AvaliacaoService : IAvaliacaoService
{
    private readonly ParakiDbContext _db;
    private readonly UserManager<Usuario> _userManager;

    public AvaliacaoService(ParakiDbContext db, UserManager<Usuario> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IEnumerable<AvaliacaoDto>> ListarPorBicicletarioAsync(Guid bicicletarioId)
    {
        var existe = await _db.Bicicletarios.AnyAsync(b => b.Id == bicicletarioId);
        if (!existe)
            throw new NotFoundException($"Bicicletário {bicicletarioId} não encontrado.");

        return await _db.Avaliacoes
            .Where(a => a.BicicletarioId == bicicletarioId)
            .OrderByDescending(a => a.CriadoEm)
            .Select(a => new AvaliacaoDto
            {
                Id = a.Id,
                UsuarioId = a.UsuarioId,
                NomeUsuario = a.Usuario.DisplayName,
                FotoPerfilUrl = a.Usuario.FotoPerfilUrl,
                Nota = a.Nota,
                Comentario = a.Comentario,
                CriadoEm = a.CriadoEm
            })
            .ToListAsync();
    }

    public async Task<AvaliacaoDto> CriarAsync(Guid bicicletarioId, CriarAvaliacaoDto dto, Guid usuarioId)
    {
        var existe = await _db.Bicicletarios.AnyAsync(b => b.Id == bicicletarioId);
        if (!existe)
            throw new NotFoundException($"Bicicletário {bicicletarioId} não encontrado.");

        if (dto.Nota < 1 || dto.Nota > 5)
            throw new ValidationException("Nota deve ser entre 1 e 5.");

        var jaAvaliou = await _db.Avaliacoes
            .AnyAsync(a => a.BicicletarioId == bicicletarioId && a.UsuarioId == usuarioId);
        if (jaAvaliou)
            throw new ConflictException("Você já avaliou este bicicletário.");

        var avaliacao = new Avaliacao
        {
            Id = Guid.NewGuid(),
            BicicletarioId = bicicletarioId,
            UsuarioId = usuarioId,
            Nota = dto.Nota,
            Comentario = dto.Comentario,
            CriadoEm = DateTime.UtcNow
        };

        _db.Avaliacoes.Add(avaliacao);
        await _db.SaveChangesAsync();

        var usuario = await _userManager.FindByIdAsync(usuarioId.ToString());
        return new AvaliacaoDto
        {
            Id = avaliacao.Id,
            UsuarioId = avaliacao.UsuarioId,
            NomeUsuario = usuario?.DisplayName ?? string.Empty,
            FotoPerfilUrl = usuario?.FotoPerfilUrl,
            Nota = avaliacao.Nota,
            Comentario = avaliacao.Comentario,
            CriadoEm = avaliacao.CriadoEm
        };
    }
}
