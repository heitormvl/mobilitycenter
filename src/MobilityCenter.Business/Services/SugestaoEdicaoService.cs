using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MobilityCenter.Business.Interfaces;
using MobilityCenter.Repositories.Context;
using MobilityCenter.Shared.DTOs.Bicicletario;
using MobilityCenter.Shared.DTOs.SugestaoEdicao;
using MobilityCenter.Shared.Enums;
using MobilityCenter.Shared.Exceptions;
using MobilityCenter.Shared.Models;
using NetTopologySuite.Geometries;

namespace MobilityCenter.Business.Services;

public class SugestaoEdicaoService : ISugestaoEdicaoService
{
    private readonly MobilityCenterDbContext _db;
    private readonly IBicicletarioService _bicicletarioService;

    public SugestaoEdicaoService(MobilityCenterDbContext db, IBicicletarioService bicicletarioService)
    {
        _db = db;
        _bicicletarioService = bicicletarioService;
    }

    public async Task<List<SugestaoEdicaoDto>> ListarPorBicicletarioAsync(Guid bicicletarioId, Guid revisorId)
    {
        var bicicletario = await _db.Bicicletarios.FindAsync(bicicletarioId)
            ?? throw new NotFoundException($"Bicicletário {bicicletarioId} não encontrado.");

        var revisor = await _db.Users.FindAsync(revisorId)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (revisor.Type != TipoUsuario.Admin && bicicletario.OperadorId != revisorId)
            throw new UnauthorizedException("Sem permissão para visualizar sugestões deste bicicletário.");

        return await _db.SugestoesEdicao
            .Include(s => s.Autor)
            .Include(s => s.Revisor)
            .Include(s => s.Bicicletario)
            .Where(s => s.BicicletarioId == bicicletarioId)
            .OrderByDescending(s => s.CriadoEm)
            .Select(s => MapearDto(s))
            .ToListAsync();
    }

    public async Task<BicicletarioDetalheDto> AprovarAsync(Guid sugestaoId, Guid revisorId)
    {
        var sugestao = await _db.SugestoesEdicao
            .Include(s => s.Bicicletario)
            .FirstOrDefaultAsync(s => s.Id == sugestaoId)
            ?? throw new NotFoundException($"Sugestão {sugestaoId} não encontrada.");

        if (sugestao.Status != StatusSugestao.Pendente)
            throw new ConflictException("Esta sugestão já foi avaliada.");

        var revisor = await _db.Users.FindAsync(revisorId)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (revisor.Type != TipoUsuario.Admin && sugestao.Bicicletario.OperadorId != revisorId)
            throw new UnauthorizedException("Sem permissão para aprovar esta sugestão.");

        var dto = JsonSerializer.Deserialize<AtualizarBicicletarioDto>(sugestao.DadosEdicao)
            ?? throw new AppException("Dados da sugestão corrompidos.", 500);

        AplicarEdicao(sugestao.Bicicletario, dto);

        if (dto.Horarios != null)
        {
            var existentes = await _db.HorariosFuncionamento
                .Where(h => h.BicicletarioId == sugestao.BicicletarioId)
                .ToListAsync();
            _db.HorariosFuncionamento.RemoveRange(existentes);
            foreach (var h in dto.Horarios)
                _db.HorariosFuncionamento.Add(new HorarioFuncionamento
                {
                    Id = Guid.NewGuid(),
                    BicicletarioId = sugestao.BicicletarioId,
                    DiaSemana = h.DiaSemana,
                    HoraAbertura = TimeOnly.Parse(h.HoraAbertura, System.Globalization.CultureInfo.InvariantCulture),
                    HoraFechamento = TimeOnly.Parse(h.HoraFechamento, System.Globalization.CultureInfo.InvariantCulture),
                });
        }

        sugestao.Bicicletario.AtualizadoEm = DateTime.UtcNow;

        sugestao.Status = StatusSugestao.Aprovada;
        sugestao.RevisorId = revisorId;
        sugestao.AvaliadaEm = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return await _bicicletarioService.ObterPorIdAsync(sugestao.BicicletarioId);
    }

    public async Task<SugestaoEdicaoDto> RejeitarAsync(Guid sugestaoId, Guid revisorId, string? motivo)
    {
        var sugestao = await _db.SugestoesEdicao
            .Include(s => s.Bicicletario)
            .Include(s => s.Autor)
            .Include(s => s.Revisor)
            .FirstOrDefaultAsync(s => s.Id == sugestaoId)
            ?? throw new NotFoundException($"Sugestão {sugestaoId} não encontrada.");

        if (sugestao.Status != StatusSugestao.Pendente)
            throw new ConflictException("Esta sugestão já foi avaliada.");

        var revisor = await _db.Users.FindAsync(revisorId)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (revisor.Type != TipoUsuario.Admin && sugestao.Bicicletario.OperadorId != revisorId)
            throw new UnauthorizedException("Sem permissão para rejeitar esta sugestão.");

        sugestao.Status = StatusSugestao.Rejeitada;
        sugestao.RevisorId = revisorId;
        sugestao.MotivoRejeicao = motivo;
        sugestao.AvaliadaEm = DateTime.UtcNow;
        sugestao.Revisor = revisor;

        await _db.SaveChangesAsync();

        return MapearDto(sugestao);
    }

    private static void AplicarEdicao(Bicicletario b, AtualizarBicicletarioDto dto)
    {
        if (dto.Nome != null) b.Nome = dto.Nome;
        if (dto.Latitude.HasValue) b.Latitude = dto.Latitude.Value;
        if (dto.Longitude.HasValue) b.Longitude = dto.Longitude.Value;
        if (dto.Latitude.HasValue || dto.Longitude.HasValue)
            b.Location = new Point(b.Longitude, b.Latitude) { SRID = 4326 };

        if (dto.TemTomada.HasValue) b.TemTomada = dto.TemTomada.Value;
        if (dto.TemCalibrador.HasValue) b.TemCalibrador = dto.TemCalibrador.Value;
        if (dto.TemVestiario.HasValue) b.TemVestiario = dto.TemVestiario.Value;
        if (dto.TemArmario.HasValue) b.TemArmario = dto.TemArmario.Value;
        if (dto.TemEspacoManutencao.HasValue) b.TemEspacoManutencao = dto.TemEspacoManutencao.Value;
        if (dto.TemCadeado.HasValue) b.TemCadeado = dto.TemCadeado.Value;
        if (dto.TemBanheiro.HasValue) b.TemBanheiro = dto.TemBanheiro.Value;

        if (dto.AcessoLivre.HasValue) b.AcessoLivre = dto.AcessoLivre.Value;
        if (dto.AcessoPago.HasValue) b.AcessoPago = dto.AcessoPago.Value;
        if (dto.AcessoCadastro.HasValue) b.AcessoCadastro = dto.AcessoCadastro.Value;
        if (dto.AcessoMensal.HasValue) b.AcessoMensal = dto.AcessoMensal.Value;

        if (dto.VeiculosSuportados.HasValue) b.VeiculosSuportados = dto.VeiculosSuportados.Value;
    }

    private static SugestaoEdicaoDto MapearDto(SugestaoEdicao s) => new()
    {
        Id = s.Id,
        BicicletarioId = s.BicicletarioId,
        NomeBicicletario = s.Bicicletario?.Nome ?? string.Empty,
        AutorId = s.AutorId,
        NomeAutor = s.Autor?.DisplayName ?? string.Empty,
        RevisorId = s.RevisorId,
        NomeRevisor = s.Revisor?.DisplayName,
        Status = s.Status,
        DadosEdicao = JsonSerializer.Deserialize<AtualizarBicicletarioDto>(s.DadosEdicao) ?? new(),
        MotivoRejeicao = s.MotivoRejeicao,
        CriadoEm = s.CriadoEm,
        AvaliadaEm = s.AvaliadaEm
    };
}
