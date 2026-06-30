using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Paraki.Business.Interfaces;
using Paraki.Repositories.Context;
using Paraki.Shared.DTOs.Bicicletario;
using Paraki.Shared.DTOs.SugestaoEdicao;
using Paraki.Shared.Enums;
using Paraki.Shared.Exceptions;
using Paraki.Shared.Models;
using NetTopologySuite.Geometries;

namespace Paraki.Business.Services;

public class SugestaoEdicaoService : ISugestaoEdicaoService
{
    private readonly ParakiDbContext _db;
    private readonly IBicicletarioService _bicicletarioService;
    private readonly IFotoStorageService _fotoStorage;
    private readonly IAuditoriaService _auditoria;

    public SugestaoEdicaoService(ParakiDbContext db, IBicicletarioService bicicletarioService, IFotoStorageService fotoStorage, IAuditoriaService auditoria)
    {
        _db = db;
        _bicicletarioService = bicicletarioService;
        _fotoStorage = fotoStorage;
        _auditoria = auditoria;
    }

    public async Task<List<SugestaoEdicaoDto>> ListarPendentesAsync(Guid adminId)
    {
        var admin = await _db.Users.FindAsync(adminId)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (admin.Type != TipoUsuario.Admin && admin.Type != TipoUsuario.Moderador)
            throw new UnauthorizedException("Sem permissão para listar sugestões pendentes.");

        return await _db.SugestoesEdicao
            .Include(s => s.Autor)
            .Include(s => s.Bicicletario)
            .Include(s => s.Revisor)
            .Where(s => s.Status == StatusSugestao.Pendente)
            .OrderBy(s => s.CriadoEm)
            .Select(s => MapearDto(s))
            .ToListAsync();
    }

    public async Task<int> ContarPendentesAsync(Guid adminId)
    {
        var admin = await _db.Users.FindAsync(adminId)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (admin.Type != TipoUsuario.Admin && admin.Type != TipoUsuario.Moderador)
            throw new UnauthorizedException("Sem permissão para consultar contagem de sugestões.");

        return await _db.SugestoesEdicao.CountAsync(s => s.Status == StatusSugestao.Pendente);
    }

    public async Task<SugestaoEdicaoDto> AdicionarFotoAsync(Guid sugestaoId, Guid autorId, IFormFile foto)
    {
        var sugestao = await _db.SugestoesEdicao
            .Include(s => s.Autor)
            .Include(s => s.Bicicletario)
            .FirstOrDefaultAsync(s => s.Id == sugestaoId)
            ?? throw new NotFoundException("Sugestão não encontrada.");

        if (sugestao.AutorId != autorId)
            throw new UnauthorizedException("Sem permissão para adicionar foto a esta sugestão.");

        if (sugestao.Status != StatusSugestao.Pendente)
            throw new ConflictException("Não é possível modificar uma sugestão já avaliada.");

        // Deletar foto anterior se existir
        if (sugestao.ComprovanteFotoKey != null && Guid.TryParse(sugestao.ComprovanteFotoKey, out var fotoAnteriorId))
            await _fotoStorage.DeleteFotoComprovanteAsync(sugestaoId, fotoAnteriorId);

        var novaFotoId = Guid.NewGuid();
        using var stream = foto.OpenReadStream();
        await _fotoStorage.UploadFotoComprovanteAsync(sugestaoId, novaFotoId, stream, foto.ContentType);

        sugestao.ComprovanteFotoKey = novaFotoId.ToString();
        await _db.SaveChangesAsync();

        return MapearDto(sugestao);
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
            .Include(s => s.Bicicletario).ThenInclude(b => b.Horarios)
            .Include(s => s.Autor)
            .FirstOrDefaultAsync(s => s.Id == sugestaoId)
            ?? throw new NotFoundException($"Sugestão {sugestaoId} não encontrada.");

        if (sugestao.Status != StatusSugestao.Pendente)
            throw new ConflictException("Esta sugestão já foi avaliada.");

        var revisor = await _db.Users.FindAsync(revisorId)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (revisor.Type != TipoUsuario.Admin && revisor.Type != TipoUsuario.Moderador && sugestao.Bicicletario.OperadorId != revisorId)
            throw new UnauthorizedException("Sem permissão para aprovar esta sugestão.");

        var dto = JsonSerializer.Deserialize<AtualizarBicicletarioDto>(sugestao.DadosEdicao)
            ?? throw new AppException("Dados da sugestão corrompidos.", 500);

        var snapAntes = _auditoria.CriarSnapshot(sugestao.Bicicletario);

        if (!sugestao.AplicadaAutomaticamente)
        {
            // Padrão: apply the edit now
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

            // Award +1 to Padrão author
            if (sugestao.Autor != null)
                sugestao.Autor.PontosAprovados += 1;
        }
        // For AplicadaAutomaticamente (Prata): edit is already live, no re-apply needed

        // Deletar foto de comprovante ao aprovar — não é mais necessária
        if (sugestao.ComprovanteFotoKey != null && Guid.TryParse(sugestao.ComprovanteFotoKey, out var fotoComprovanteId))
            await _fotoStorage.DeleteFotoComprovanteAsync(sugestao.Id, fotoComprovanteId);
        sugestao.ComprovanteFotoKey = null;

        sugestao.Status = StatusSugestao.Aprovada;
        sugestao.RevisorId = revisorId;
        sugestao.AvaliadaEm = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Audit: after-snapshot for non-auto (auto already audited on apply)
        if (!sugestao.AplicadaAutomaticamente)
        {
            var bAfter = await _db.Bicicletarios.Include(b => b.Horarios).FirstAsync(b => b.Id == sugestao.BicicletarioId);
            var snapDepois = _auditoria.CriarSnapshot(bAfter);
            await _auditoria.RegistrarAsync(TipoAcaoAuditoria.Aprovacao, revisorId, revisor.DisplayName,
                sugestao.BicicletarioId, snapAntes, snapDepois, sugestaoId: sugestaoId);
            await _db.SaveChangesAsync();
        }

        return await _bicicletarioService.ObterPorIdAsync(sugestao.BicicletarioId);
    }

    public async Task<SugestaoEdicaoDto> RejeitarAsync(Guid sugestaoId, Guid revisorId, string? motivo)
    {
        var sugestao = await _db.SugestoesEdicao
            .Include(s => s.Bicicletario).ThenInclude(b => b.Horarios)
            .Include(s => s.Autor)
            .Include(s => s.Revisor)
            .FirstOrDefaultAsync(s => s.Id == sugestaoId)
            ?? throw new NotFoundException($"Sugestão {sugestaoId} não encontrada.");

        if (sugestao.Status != StatusSugestao.Pendente)
            throw new ConflictException("Esta sugestão já foi avaliada.");

        var revisor = await _db.Users.FindAsync(revisorId)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (revisor.Type != TipoUsuario.Admin && revisor.Type != TipoUsuario.Moderador && sugestao.Bicicletario.OperadorId != revisorId)
            throw new UnauthorizedException("Sem permissão para rejeitar esta sugestão.");

        if (sugestao.AplicadaAutomaticamente && sugestao.DadosAnteriores != null)
        {
            // Prata: revert the auto-applied edit
            var dadosAnteriores = JsonSerializer.Deserialize<AtualizarBicicletarioDto>(sugestao.DadosAnteriores)
                ?? throw new AppException("Snapshot anterior corrompido.", 500);

            var snapAntes = _auditoria.CriarSnapshot(sugestao.Bicicletario);
            AplicarEdicao(sugestao.Bicicletario, dadosAnteriores);

            if (dadosAnteriores.Horarios != null)
            {
                var existentes = await _db.HorariosFuncionamento
                    .Where(h => h.BicicletarioId == sugestao.BicicletarioId)
                    .ToListAsync();
                _db.HorariosFuncionamento.RemoveRange(existentes);
                foreach (var h in dadosAnteriores.Horarios)
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

            // Deduct -1 point from Prata author
            if (sugestao.Autor != null)
                sugestao.Autor.PontosAprovados = Math.Max(0, sugestao.Autor.PontosAprovados - 1);

            await _db.SaveChangesAsync();

            var bAfter = await _db.Bicicletarios.Include(b => b.Horarios).FirstAsync(b => b.Id == sugestao.BicicletarioId);
            var snapDepois = _auditoria.CriarSnapshot(bAfter);
            await _auditoria.RegistrarAsync(TipoAcaoAuditoria.Rejeicao, revisorId, revisor.DisplayName,
                sugestao.BicicletarioId, snapAntes, snapDepois, observacao: motivo, sugestaoId: sugestaoId);
        }

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
        Comprovante = s.Comprovante,
        ComprovanteFotoKey = s.ComprovanteFotoKey,
        AplicadaAutomaticamente = s.AplicadaAutomaticamente,
        TierAutor = s.Autor?.Tier ?? TipoTier.Padrao,
        CriadoEm = s.CriadoEm,
        AvaliadaEm = s.AvaliadaEm
    };
}
