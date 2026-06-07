using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MobilityCenter.Business.Interfaces;
using MobilityCenter.Repositories.Context;
using MobilityCenter.Shared.DTOs.Avaliacao;
using MobilityCenter.Shared.DTOs.Bicicletario;
using MobilityCenter.Shared.DTOs.Usuario;
using MobilityCenter.Shared.Enums;
using MobilityCenter.Shared.Exceptions;
using MobilityCenter.Shared.Models;

namespace MobilityCenter.Business.Services;

public class UsuarioService : IUsuarioService
{
    private readonly MobilityCenterDbContext _db;
    private readonly UserManager<Usuario> _userManager;
    private readonly IFotoStorageService _fotoStorage;

    public UsuarioService(MobilityCenterDbContext db, UserManager<Usuario> userManager, IFotoStorageService fotoStorage)
    {
        _db = db;
        _userManager = userManager;
        _fotoStorage = fotoStorage;
    }

    public async Task<UsuarioPerfilDto> ObterPerfilAsync(Guid usuarioId)
    {
        var usuario = await _userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new NotFoundException("Usuário não encontrado.");

        var totalAvaliacoes = await _db.Avaliacoes.CountAsync(a => a.UsuarioId == usuarioId);
        var totalAdicionados = await _db.Bicicletarios.CountAsync(b => b.OperadorId == usuarioId);

        return new UsuarioPerfilDto
        {
            Usuario = new UsuarioDto
            {
                Id = usuario.Id,
                DisplayName = usuario.DisplayName,
                Email = usuario.Email!,
                Type = usuario.Type,
                CreatedAt = usuario.CreatedAt,
                FotoPerfilUrl = usuario.FotoPerfilUrl
            },
            TotalAvaliacoes = totalAvaliacoes,
            TotalAdicionados = totalAdicionados
        };
    }

    public async Task<string> AtualizarFotoPerfilAsync(Guid usuarioId, Stream imageStream, string contentType)
    {
        var usuario = await _userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new NotFoundException("Usuário não encontrado.");

        var url = await _fotoStorage.UploadFotoPerfilAsync(usuarioId, imageStream, contentType);

        usuario.FotoPerfilUrl = url;
        await _userManager.UpdateAsync(usuario);

        return url;
    }

    public async Task<IEnumerable<AvaliacaoDto>> ObterAvaliacoesAsync(Guid usuarioId)
    {
        return await _db.Avaliacoes
            .Where(a => a.UsuarioId == usuarioId)
            .OrderByDescending(a => a.CriadoEm)
            .Select(a => new AvaliacaoDto
            {
                Id = a.Id,
                UsuarioId = a.UsuarioId,
                NomeUsuario = a.Usuario.DisplayName,
                Nota = a.Nota,
                Comentario = a.Comentario,
                CriadoEm = a.CriadoEm
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<BicicletarioResumoDto>> ObterBicicletariosAsync(Guid usuarioId)
    {
        return await _db.Bicicletarios
            .Where(b => b.OperadorId == usuarioId)
            .Select(b => new BicicletarioResumoDto
            {
                Id = b.Id,
                Nome = b.Nome,
                Latitude = b.Latitude,
                Longitude = b.Longitude,
                NotaMedia = b.Avaliacoes.Select(a => (double?)a.Nota).Average() ?? 0,
                TotalAvaliacoes = b.Avaliacoes.Count,
                VeiculosSuportados = b.VeiculosSuportados
            })
            .ToListAsync();
    }
}
