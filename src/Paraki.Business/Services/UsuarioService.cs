using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Paraki.Business.Interfaces;
using Paraki.Repositories.Context;
using Paraki.Shared.DTOs.Avaliacao;
using Paraki.Shared.DTOs.Bicicletario;
using Paraki.Shared.DTOs.Usuario;
using Paraki.Shared.Enums;
using Paraki.Shared.Exceptions;
using Paraki.Shared.Models;

namespace Paraki.Business.Services;

public class UsuarioService : IUsuarioService
{
    private readonly ParakiDbContext _db;
    private readonly UserManager<Usuario> _userManager;
    private readonly IFotoStorageService _fotoStorage;

    public UsuarioService(ParakiDbContext db, UserManager<Usuario> userManager, IFotoStorageService fotoStorage)
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

    public async Task<UsuarioDto> AtualizarPerfilAsync(Guid usuarioId, AtualizarPerfilDto dto)
    {
        var usuario = await _userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new NotFoundException("Usuário não encontrado.");

        var displayName = dto.DisplayName?.Trim() ?? string.Empty;
        var email = dto.Email?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ValidationException("O nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("O e-mail é obrigatório.");

        if (!string.Equals(email, usuario.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existente = await _userManager.FindByEmailAsync(email);
            if (existente != null && existente.Id != usuarioId)
                throw new ConflictException("E-mail já cadastrado.");

            var setEmail = await _userManager.SetEmailAsync(usuario, email);
            if (!setEmail.Succeeded)
                throw new ValidationException(string.Join(", ", setEmail.Errors.Select(e => e.Description)));

            var setUserName = await _userManager.SetUserNameAsync(usuario, email);
            if (!setUserName.Succeeded)
                throw new ValidationException(string.Join(", ", setUserName.Errors.Select(e => e.Description)));
        }

        usuario.DisplayName = displayName;
        var resultado = await _userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded)
            throw new ValidationException(string.Join(", ", resultado.Errors.Select(e => e.Description)));

        return new UsuarioDto
        {
            Id = usuario.Id,
            DisplayName = usuario.DisplayName,
            Email = usuario.Email!,
            Type = usuario.Type,
            CreatedAt = usuario.CreatedAt,
            FotoPerfilUrl = usuario.FotoPerfilUrl
        };
    }

    public async Task AlterarSenhaAsync(Guid usuarioId, AlterarSenhaDto dto)
    {
        var usuario = await _userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (string.IsNullOrWhiteSpace(dto.NovaSenha))
            throw new ValidationException("A nova senha é obrigatória.");

        var resultado = await _userManager.ChangePasswordAsync(usuario, dto.SenhaAtual, dto.NovaSenha);
        if (!resultado.Succeeded)
        {
            if (resultado.Errors.Any(e => e.Code == "PasswordMismatch"))
                throw new ValidationException("Senha atual incorreta.");

            throw new ValidationException(string.Join(", ", resultado.Errors.Select(e => e.Description)));
        }
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

    public async Task ExcluirContaAsync(Guid usuarioId)
    {
        var usuario = await _userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new NotFoundException("Usuário não encontrado.");

        await _fotoStorage.DeleteFotoPerfilAsync(usuarioId);

        var resultado = await _userManager.DeleteAsync(usuario);
        if (!resultado.Succeeded)
            throw new ValidationException(string.Join(", ", resultado.Errors.Select(e => e.Description)));
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
