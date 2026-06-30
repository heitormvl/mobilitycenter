using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Paraki.Business.Filters;
using Paraki.Business.Interfaces;
using Paraki.Repositories.Context;
using Paraki.Shared.DTOs.Avaliacao;
using Paraki.Shared.DTOs.Bicicletario;
using Paraki.Shared.DTOs.LogAuditoria;
using Paraki.Shared.DTOs.SugestaoEdicao;
using Paraki.Shared.Enums;
using Paraki.Shared.Exceptions;
using Paraki.Shared.Models;
using NetTopologySuite.Geometries;

namespace Paraki.Business.Services;

public class BicicletarioService : IBicicletarioService
{
    private readonly ParakiDbContext _db;
    private readonly IAuditoriaService _auditoria;

    public BicicletarioService(ParakiDbContext db, IAuditoriaService auditoria)
    {
        _db = db;
        _auditoria = auditoria;
    }

    public async Task<IEnumerable<BicicletarioResumoDto>> ListarAsync(BicicletarioFiltros filtros)
    {
        var query = filtros.IncluirOcultas
            ? _db.Bicicletarios.IgnoreQueryFilters().AsQueryable()
            : _db.Bicicletarios.Where(b => b.StatusAprovacao != StatusBicicletario.Pendente).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtros.Q))
            query = query.Where(b => b.Nome.Contains(filtros.Q));

        if (filtros.AcessoLivre == true)
            query = query.Where(b => b.AcessoLivre);

        if (filtros.TemTomada == true)
            query = query.Where(b => b.TemTomada);

        if (filtros.TipoVeiculo.HasValue && filtros.TipoVeiculo != TipoVeiculo.Nenhum)
        {
            var tipo = filtros.TipoVeiculo.Value;
            query = query.Where(b => (b.VeiculosSuportados & tipo) != TipoVeiculo.Nenhum);
        }

        Point? ponto = null;
        if (filtros.Lat.HasValue && filtros.Lon.HasValue)
        {
            ponto = new Point(filtros.Lon.Value, filtros.Lat.Value) { SRID = 4326 };

            if (filtros.RaioKm.HasValue)
            {
                var raioGraus = filtros.RaioKm.Value / 111.0;
                query = query.Where(b => b.Location != null && b.Location.IsWithinDistance(ponto, raioGraus));
            }
        }

        var pontoCapturado = ponto;
        query = filtros.OrderBy switch
        {
            "nota"      => query.OrderByDescending(b => b.Avaliacoes.Select(a => (double?)a.Nota).Average() ?? 0),
            "avaliacoes" => query.OrderByDescending(b => b.Avaliacoes.Count),
            _ when pontoCapturado != null => query.OrderBy(b =>
                b.Location == null ? double.MaxValue : b.Location.Distance(pontoCapturado)),
            _ => query.OrderBy(b => b.Nome)
        };

        var rows = await query
            .Skip((filtros.Page - 1) * filtros.PageSize)
            .Take(filtros.PageSize)
            .Select(b => new
            {
                b.Id, b.Nome, b.Latitude, b.Longitude,
                NotaMedia = b.Avaliacoes.Average(a => (double?)a.Nota) ?? 0.0,
                TotalAvaliacoes = b.Avaliacoes.Count(),
                b.VeiculosSuportados,
                b.TemTomada, b.TemCalibrador, b.TemVestiario, b.TemArmario,
                b.TemEspacoManutencao, b.TemCadeado, b.TemBanheiro,
                b.AcessoLivre, b.AcessoPago, b.AcessoCadastro, b.AcessoMensal,
                IsDeleted = b.Deletado,
                b.StatusAprovacao,
                NomeCriador = b.Criador != null ? b.Criador.DisplayName : null,
                TierCriadorPontos = b.Criador != null ? (int?)b.Criador.PontosAprovados : null,
                TierCriadorOverride = b.Criador != null ? b.Criador.TierOverride : null,
                Horarios = b.Horarios
                    .OrderBy(h => h.DiaSemana)
                    .Select(h => new { h.DiaSemana, h.HoraAbertura, h.HoraFechamento })
                    .ToList(),
                CapaId = b.Fotos.Where(f => f.IsCapa).Select(f => (Guid?)f.Id).FirstOrDefault()
            })
            .ToListAsync();

        return rows.Select(b => new BicicletarioResumoDto
        {
            Id = b.Id, Nome = b.Nome, Latitude = b.Latitude, Longitude = b.Longitude,
            NotaMedia = b.NotaMedia, TotalAvaliacoes = b.TotalAvaliacoes,
            VeiculosSuportados = b.VeiculosSuportados,
            TemTomada = b.TemTomada, TemCalibrador = b.TemCalibrador, TemVestiario = b.TemVestiario,
            TemArmario = b.TemArmario, TemEspacoManutencao = b.TemEspacoManutencao,
            TemCadeado = b.TemCadeado, TemBanheiro = b.TemBanheiro,
            AcessoLivre = b.AcessoLivre, AcessoPago = b.AcessoPago,
            AcessoCadastro = b.AcessoCadastro, AcessoMensal = b.AcessoMensal,
            IsDeleted = b.IsDeleted,
            StatusAprovacao = b.StatusAprovacao,
            NomeCriador = b.NomeCriador,
            TierCriador = b.TierCriadorOverride ?? (b.TierCriadorPontos.HasValue
                ? ComputeTier(b.TierCriadorPontos.Value)
                : null),
            Horarios = b.Horarios.Select(h => new HorarioFuncionamentoDto
            {
                DiaSemana = h.DiaSemana,
                HoraAbertura = h.HoraAbertura.ToString("HH:mm"),
                HoraFechamento = h.HoraFechamento.ToString("HH:mm")
            }).ToList(),
            CapaUrl = b.CapaId.HasValue ? $"/api/fotos/bicicletario/{b.Id}/{b.CapaId.Value}" : null
        });
    }

    public async Task<BicicletarioDetalheDto> ObterPorIdAsync(Guid id)
    {
        var b = await _db.Bicicletarios
            .IgnoreQueryFilters()
            .Include(b => b.Avaliacoes).ThenInclude(a => a.Usuario)
            .Include(b => b.Operador)
            .Include(b => b.Criador)
            .Include(b => b.Horarios)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException($"Bicicletário {id} não encontrado.");

        return MapearDetalhe(b);
    }

    public async Task<BicicletarioDetalheDto> CriarAsync(CriarBicicletarioDto dto, Guid usuarioId)
    {
        var usuario = await _db.Users.FindAsync(usuarioId)
            ?? throw new NotFoundException("Usuário não encontrado.");

        var tier = usuario.Tier;
        var status = tier switch
        {
            TipoTier.Ouro  => StatusBicicletario.Aprovado,
            TipoTier.Prata => StatusBicicletario.AutoAprovado,
            _              => StatusBicicletario.Pendente,
        };

        var bicicletario = new Bicicletario
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Location = new Point(dto.Longitude, dto.Latitude) { SRID = 4326 },
            TemTomada = dto.TemTomada,
            TemCalibrador = dto.TemCalibrador,
            TemVestiario = dto.TemVestiario,
            TemArmario = dto.TemArmario,
            TemEspacoManutencao = dto.TemEspacoManutencao,
            TemCadeado = dto.TemCadeado,
            TemBanheiro = dto.TemBanheiro,
            AcessoLivre = dto.AcessoLivre,
            AcessoPago = dto.AcessoPago,
            AcessoCadastro = dto.AcessoCadastro,
            AcessoMensal = dto.AcessoMensal,
            VeiculosSuportados = dto.VeiculosSuportados,
            CriadorId = usuarioId,
            StatusAprovacao = status,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow,
        };

        _db.Bicicletarios.Add(bicicletario);

        if (dto.Horarios != null)
            foreach (var h in dto.Horarios)
                _db.HorariosFuncionamento.Add(new HorarioFuncionamento
                {
                    Id = Guid.NewGuid(),
                    BicicletarioId = bicicletario.Id,
                    DiaSemana = h.DiaSemana,
                    HoraAbertura = TimeOnly.Parse(h.HoraAbertura, System.Globalization.CultureInfo.InvariantCulture),
                    HoraFechamento = TimeOnly.Parse(h.HoraFechamento, System.Globalization.CultureInfo.InvariantCulture),
                });

        // Award points for Prata and Ouro (no admin approval needed)
        if (tier == TipoTier.Prata || tier == TipoTier.Ouro)
            usuario.PontosAprovados += 2;

        // Build snapshot with DTO horarios since nav property is not yet saved
        var horariosJson = dto.Horarios != null
            ? JsonSerializer.Serialize(dto.Horarios.Select(h => new { DiaSemana = (int)h.DiaSemana, HoraAbertura = h.HoraAbertura, HoraFechamento = h.HoraFechamento }))
            : "[]";

        var snapDepois = _auditoria.CriarSnapshot(bicicletario);
        snapDepois.HorariosJson = horariosJson;

        await _auditoria.RegistrarAsync(TipoAcaoAuditoria.Criacao, usuarioId, usuario.DisplayName,
            bicicletario.Id, antes: null, depois: snapDepois);

        await _db.SaveChangesAsync();

        return await ObterPorIdAsync(bicicletario.Id);
    }

    public async Task<ResultadoAtualizacaoDto> AtualizarAsync(Guid id, AtualizarBicicletarioDto dto, Guid usuarioId)
    {
        var bicicletario = await _db.Bicicletarios
            .Include(b => b.Horarios)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException($"Bicicletário {id} não encontrado.");

        var usuario = await _db.Users.FindAsync(usuarioId)
            ?? throw new NotFoundException("Usuário não encontrado.");

        bool ehDono = bicicletario.OperadorId == usuarioId;
        bool podeEditarDireto = usuario.Type == TipoUsuario.Admin
            || (usuario.Type == TipoUsuario.Operador && ehDono);

        if (podeEditarDireto)
        {
            var snapAntes = _auditoria.CriarSnapshot(bicicletario);
            AplicarEdicao(bicicletario, dto);

            if (dto.Horarios != null)
            {
                var existentes = await _db.HorariosFuncionamento
                    .Where(h => h.BicicletarioId == id)
                    .ToListAsync();
                _db.HorariosFuncionamento.RemoveRange(existentes);
                foreach (var h in dto.Horarios)
                    _db.HorariosFuncionamento.Add(new HorarioFuncionamento
                    {
                        Id = Guid.NewGuid(), BicicletarioId = id,
                        DiaSemana = h.DiaSemana,
                        HoraAbertura = TimeOnly.Parse(h.HoraAbertura, System.Globalization.CultureInfo.InvariantCulture),
                        HoraFechamento = TimeOnly.Parse(h.HoraFechamento, System.Globalization.CultureInfo.InvariantCulture),
                    });
            }

            bicicletario.AtualizadoEm = DateTime.UtcNow;

            // Reload horarios for after-snapshot
            await _db.SaveChangesAsync();

            var bAfter = await _db.Bicicletarios.Include(b => b.Horarios).FirstAsync(b => b.Id == id);
            var snapDepois = _auditoria.CriarSnapshot(bAfter);
            await _auditoria.RegistrarAsync(TipoAcaoAuditoria.Edicao, usuarioId, usuario.DisplayName,
                id, snapAntes, snapDepois);
            await _db.SaveChangesAsync();

            return new ResultadoAtualizacaoDto
            {
                EditadoDireto = true,
                Bicicletario = await ObterPorIdAsync(id)
            };
        }

        // Prata: apply immediately, create pending suggestion for double-check
        if (usuario.Tier == TipoTier.Prata)
        {
            // Capture before-state for revert + audit
            var dadosAnterioresDto = new AtualizarBicicletarioDto
            {
                Nome = bicicletario.Nome,
                Latitude = bicicletario.Latitude,
                Longitude = bicicletario.Longitude,
                TemTomada = bicicletario.TemTomada,
                TemCalibrador = bicicletario.TemCalibrador,
                TemVestiario = bicicletario.TemVestiario,
                TemArmario = bicicletario.TemArmario,
                TemEspacoManutencao = bicicletario.TemEspacoManutencao,
                TemCadeado = bicicletario.TemCadeado,
                TemBanheiro = bicicletario.TemBanheiro,
                AcessoLivre = bicicletario.AcessoLivre,
                AcessoPago = bicicletario.AcessoPago,
                AcessoCadastro = bicicletario.AcessoCadastro,
                AcessoMensal = bicicletario.AcessoMensal,
                VeiculosSuportados = bicicletario.VeiculosSuportados,
                Horarios = bicicletario.Horarios.Select(h => new HorarioFuncionamentoDto
                {
                    DiaSemana = h.DiaSemana,
                    HoraAbertura = h.HoraAbertura.ToString("HH:mm"),
                    HoraFechamento = h.HoraFechamento.ToString("HH:mm"),
                }).ToList(),
            };

            var snapAntes = _auditoria.CriarSnapshot(bicicletario);
            AplicarEdicao(bicicletario, dto);

            if (dto.Horarios != null)
            {
                var existentes = await _db.HorariosFuncionamento
                    .Where(h => h.BicicletarioId == id)
                    .ToListAsync();
                _db.HorariosFuncionamento.RemoveRange(existentes);
                foreach (var h in dto.Horarios)
                    _db.HorariosFuncionamento.Add(new HorarioFuncionamento
                    {
                        Id = Guid.NewGuid(), BicicletarioId = id,
                        DiaSemana = h.DiaSemana,
                        HoraAbertura = TimeOnly.Parse(h.HoraAbertura, System.Globalization.CultureInfo.InvariantCulture),
                        HoraFechamento = TimeOnly.Parse(h.HoraFechamento, System.Globalization.CultureInfo.InvariantCulture),
                    });
            }

            bicicletario.AtualizadoEm = DateTime.UtcNow;
            usuario.PontosAprovados += 1;

            var sugestao = new SugestaoEdicao
            {
                Id = Guid.NewGuid(),
                BicicletarioId = id,
                AutorId = usuarioId,
                DadosEdicao = JsonSerializer.Serialize(dto),
                DadosAnteriores = JsonSerializer.Serialize(dadosAnterioresDto),
                Comprovante = dto.Comprovante,
                AplicadaAutomaticamente = true,
                CriadoEm = DateTime.UtcNow,
            };

            _db.SugestoesEdicao.Add(sugestao);
            await _db.SaveChangesAsync();

            var bAfter = await _db.Bicicletarios.Include(b => b.Horarios).FirstAsync(b => b.Id == id);
            var snapDepois = _auditoria.CriarSnapshot(bAfter);
            await _auditoria.RegistrarAsync(TipoAcaoAuditoria.Edicao, usuarioId, usuario.DisplayName,
                id, snapAntes, snapDepois, sugestaoId: sugestao.Id);
            await _db.SaveChangesAsync();

            await _db.Entry(sugestao).Reference(s => s.Autor).LoadAsync();
            await _db.Entry(sugestao).Reference(s => s.Bicicletario).LoadAsync();

            return new ResultadoAtualizacaoDto
            {
                EditadoDireto = false,
                Sugestao = new SugestaoEdicaoDto
                {
                    Id = sugestao.Id,
                    BicicletarioId = sugestao.BicicletarioId,
                    NomeBicicletario = sugestao.Bicicletario.Nome,
                    AutorId = sugestao.AutorId,
                    NomeAutor = sugestao.Autor.DisplayName,
                    Status = sugestao.Status,
                    DadosEdicao = dto,
                    AplicadaAutomaticamente = true,
                    CriadoEm = sugestao.CriadoEm,
                }
            };
        }

        // Padrão (or Operador sem dono): cria sugestão pendente
        var sugestaoPadrao = new SugestaoEdicao
        {
            Id = Guid.NewGuid(),
            BicicletarioId = id,
            AutorId = usuarioId,
            DadosEdicao = JsonSerializer.Serialize(dto),
            Comprovante = dto.Comprovante,
            CriadoEm = DateTime.UtcNow,
        };

        _db.SugestoesEdicao.Add(sugestaoPadrao);

        // Audit: record the proposed edit (before = current state)
        var snapAntesPadrao = _auditoria.CriarSnapshot(bicicletario);
        await _auditoria.RegistrarAsync(TipoAcaoAuditoria.Edicao, usuarioId, usuario.DisplayName,
            id, snapAntesPadrao, null, sugestaoId: sugestaoPadrao.Id);

        await _db.SaveChangesAsync();

        await _db.Entry(sugestaoPadrao).Reference(s => s.Autor).LoadAsync();
        await _db.Entry(sugestaoPadrao).Reference(s => s.Bicicletario).LoadAsync();

        return new ResultadoAtualizacaoDto
        {
            EditadoDireto = false,
            Sugestao = new SugestaoEdicaoDto
            {
                Id = sugestaoPadrao.Id,
                BicicletarioId = sugestaoPadrao.BicicletarioId,
                NomeBicicletario = sugestaoPadrao.Bicicletario.Nome,
                AutorId = sugestaoPadrao.AutorId,
                NomeAutor = sugestaoPadrao.Autor.DisplayName,
                Status = sugestaoPadrao.Status,
                DadosEdicao = dto,
                CriadoEm = sugestaoPadrao.CriadoEm,
            }
        };
    }

    public async Task DeletarAsync(Guid id, Guid usuarioId, TipoUsuario tipoUsuario)
    {
        var bicicletario = await _db.Bicicletarios
            .Include(b => b.Horarios)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException($"Bicicletário {id} não encontrado.");

        if (tipoUsuario != TipoUsuario.Admin && tipoUsuario != TipoUsuario.Moderador && bicicletario.OperadorId != usuarioId)
            throw new UnauthorizedException("Sem permissão para deletar este bicicletário.");

        var usuario = await _db.Users.FindAsync(usuarioId) ?? throw new NotFoundException("Usuário não encontrado.");
        var snapAntes = _auditoria.CriarSnapshot(bicicletario);

        bicicletario.Deletado = true;
        bicicletario.AtualizadoEm = DateTime.UtcNow;

        var snapDepois = _auditoria.CriarSnapshot(bicicletario);
        await _auditoria.RegistrarAsync(TipoAcaoAuditoria.Exclusao, usuarioId, usuario.DisplayName,
            id, snapAntes, snapDepois);

        await _db.SaveChangesAsync();
    }

    public async Task RestaurarAsync(Guid id, Guid adminId, TipoUsuario tipoUsuario)
    {
        if (tipoUsuario != TipoUsuario.Admin)
            throw new UnauthorizedException("Apenas administradores podem restaurar bicicletários.");

        var bicicletario = await _db.Bicicletarios.IgnoreQueryFilters()
            .Include(b => b.Horarios)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException($"Bicicletário {id} não encontrado.");

        var admin = await _db.Users.FindAsync(adminId) ?? throw new NotFoundException("Usuário não encontrado.");
        var snapAntes = _auditoria.CriarSnapshot(bicicletario);

        bicicletario.Deletado = false;
        bicicletario.AtualizadoEm = DateTime.UtcNow;

        var snapDepois = _auditoria.CriarSnapshot(bicicletario);
        await _auditoria.RegistrarAsync(TipoAcaoAuditoria.Restauracao, adminId, admin.DisplayName,
            id, snapAntes, snapDepois);

        await _db.SaveChangesAsync();
    }

    public async Task DeletarPermanenteAsync(Guid id, Guid usuarioId, TipoUsuario tipoUsuario)
    {
        if (tipoUsuario != TipoUsuario.Admin)
            throw new UnauthorizedException("Apenas administradores podem excluir permanentemente.");

        var bicicletario = await _db.Bicicletarios
            .Include(b => b.Horarios)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException($"Bicicletário {id} não encontrado.");

        var admin = await _db.Users.FindAsync(usuarioId) ?? throw new NotFoundException("Usuário não encontrado.");
        var snapAntes = _auditoria.CriarSnapshot(bicicletario);
        await _auditoria.RegistrarAsync(TipoAcaoAuditoria.Exclusao, usuarioId, admin.DisplayName,
            id, snapAntes, null, observacao: "Exclusão permanente");

        _db.Bicicletarios.Remove(bicicletario);
        await _db.SaveChangesAsync();
    }

    public async Task<List<BicicletarioPendenteDto>> ListarPendentesAsync(Guid adminId)
    {
        var admin = await _db.Users.FindAsync(adminId)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (admin.Type != TipoUsuario.Admin && admin.Type != TipoUsuario.Moderador)
            throw new UnauthorizedException("Sem permissão para listar bicicletários pendentes.");

        return await _db.Bicicletarios
            .IgnoreQueryFilters()
            .Include(b => b.Criador)
            .Include(b => b.Horarios)
            .Include(b => b.Fotos)
            .Where(b => b.StatusAprovacao == StatusBicicletario.Pendente
                     || b.StatusAprovacao == StatusBicicletario.AutoAprovado)
            .OrderBy(b => b.CriadoEm)
            .Select(b => new BicicletarioPendenteDto
            {
                Id = b.Id,
                Nome = b.Nome,
                Latitude = b.Latitude,
                Longitude = b.Longitude,
                TemTomada = b.TemTomada, TemCalibrador = b.TemCalibrador, TemVestiario = b.TemVestiario,
                TemArmario = b.TemArmario, TemEspacoManutencao = b.TemEspacoManutencao,
                TemCadeado = b.TemCadeado, TemBanheiro = b.TemBanheiro,
                AcessoLivre = b.AcessoLivre, AcessoPago = b.AcessoPago,
                AcessoCadastro = b.AcessoCadastro, AcessoMensal = b.AcessoMensal,
                VeiculosSuportados = b.VeiculosSuportados,
                StatusAprovacao = b.StatusAprovacao,
                CriadorId = b.CriadorId,
                NomeCriador = b.Criador != null ? b.Criador.DisplayName : "",
                TierCriador = b.Criador != null
                    ? (b.Criador.TierOverride ?? ComputeTier(b.Criador.PontosAprovados))
                    : TipoTier.Padrao,
                Horarios = b.Horarios.OrderBy(h => h.DiaSemana).Select(h => new HorarioFuncionamentoDto
                {
                    DiaSemana = h.DiaSemana,
                    HoraAbertura = h.HoraAbertura.ToString("HH:mm"),
                    HoraFechamento = h.HoraFechamento.ToString("HH:mm"),
                }).ToList(),
                CapaUrl = b.Fotos.Where(f => f.IsCapa).Select(f => (string?)$"/api/fotos/bicicletario/{b.Id}/{f.Id}").FirstOrDefault(),
                CriadoEm = b.CriadoEm,
            })
            .ToListAsync();
    }

    public async Task<BicicletarioPendenteDto> AprovarCriacaoAsync(Guid bicicletarioId, Guid adminId)
    {
        var admin = await _db.Users.FindAsync(adminId)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (admin.Type != TipoUsuario.Admin && admin.Type != TipoUsuario.Moderador)
            throw new UnauthorizedException("Sem permissão para aprovar bicicletários.");

        var bicicletario = await _db.Bicicletarios
            .IgnoreQueryFilters()
            .Include(b => b.Horarios)
            .Include(b => b.Criador)
            .Include(b => b.Fotos)
            .FirstOrDefaultAsync(b => b.Id == bicicletarioId)
            ?? throw new NotFoundException($"Bicicletário {bicicletarioId} não encontrado.");

        if (bicicletario.StatusAprovacao != StatusBicicletario.Pendente
            && bicicletario.StatusAprovacao != StatusBicicletario.AutoAprovado)
            throw new ConflictException("Este bicicletário já foi processado.");

        var snapAntes = _auditoria.CriarSnapshot(bicicletario);

        // Award +2 points only for Padrão creators (Prata already received points)
        if (bicicletario.StatusAprovacao == StatusBicicletario.Pendente && bicicletario.Criador != null)
            bicicletario.Criador.PontosAprovados += 2;

        bicicletario.StatusAprovacao = StatusBicicletario.Aprovado;
        bicicletario.AtualizadoEm = DateTime.UtcNow;

        var snapDepois = _auditoria.CriarSnapshot(bicicletario);
        await _auditoria.RegistrarAsync(TipoAcaoAuditoria.Aprovacao, adminId, admin.DisplayName,
            bicicletarioId, snapAntes, snapDepois);

        await _db.SaveChangesAsync();

        return new BicicletarioPendenteDto
        {
            Id = bicicletario.Id,
            Nome = bicicletario.Nome,
            StatusAprovacao = bicicletario.StatusAprovacao,
            NomeCriador = bicicletario.Criador?.DisplayName ?? "",
            TierCriador = bicicletario.Criador?.Tier ?? TipoTier.Padrao,
            CriadoEm = bicicletario.CriadoEm,
        };
    }

    public async Task RejeitarCriacaoAsync(Guid bicicletarioId, Guid adminId, string? motivo)
    {
        var admin = await _db.Users.FindAsync(adminId)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (admin.Type != TipoUsuario.Admin && admin.Type != TipoUsuario.Moderador)
            throw new UnauthorizedException("Sem permissão para rejeitar bicicletários.");

        var bicicletario = await _db.Bicicletarios
            .IgnoreQueryFilters()
            .Include(b => b.Horarios)
            .Include(b => b.Criador)
            .FirstOrDefaultAsync(b => b.Id == bicicletarioId)
            ?? throw new NotFoundException($"Bicicletário {bicicletarioId} não encontrado.");

        if (bicicletario.StatusAprovacao != StatusBicicletario.Pendente
            && bicicletario.StatusAprovacao != StatusBicicletario.AutoAprovado)
            throw new ConflictException("Este bicicletário já foi processado.");

        var snapAntes = _auditoria.CriarSnapshot(bicicletario);

        // Deduct points only if was auto-approved (Prata user already received them)
        if (bicicletario.StatusAprovacao == StatusBicicletario.AutoAprovado && bicicletario.Criador != null)
            bicicletario.Criador.PontosAprovados = Math.Max(0, bicicletario.Criador.PontosAprovados - 2);

        bicicletario.Deletado = true;
        bicicletario.AtualizadoEm = DateTime.UtcNow;

        await _auditoria.RegistrarAsync(TipoAcaoAuditoria.Rejeicao, adminId, admin.DisplayName,
            bicicletarioId, snapAntes, null, observacao: motivo);

        await _db.SaveChangesAsync();
    }

    public async Task<List<LogAuditoriaDto>> ObterAuditoriaAsync(Guid bicicletarioId, Guid adminId)
    {
        var admin = await _db.Users.FindAsync(adminId)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (admin.Type != TipoUsuario.Admin)
            throw new UnauthorizedException("Apenas administradores podem visualizar auditoria.");

        return await _db.LogsAuditoria
            .Include(l => l.SnapshotAntes)
            .Include(l => l.SnapshotDepois)
            .Where(l => l.BicicletarioId == bicicletarioId)
            .OrderByDescending(l => l.CriadoEm)
            .Select(l => new LogAuditoriaDto
            {
                Id = l.Id,
                TipoAcao = l.TipoAcao,
                UsuarioId = l.UsuarioId,
                NomeUsuario = l.NomeUsuario,
                BicicletarioId = l.BicicletarioId,
                SugestaoId = l.SugestaoId,
                Observacao = l.Observacao,
                CriadoEm = l.CriadoEm,
                SnapshotAntes = l.SnapshotAntes == null ? null : MapearSnapshot(l.SnapshotAntes),
                SnapshotDepois = l.SnapshotDepois == null ? null : MapearSnapshot(l.SnapshotDepois),
            })
            .ToListAsync();
    }

    private static SnapshotDto MapearSnapshot(SnapshotBicicletario s) => new()
    {
        Nome = s.Nome, Latitude = s.Latitude, Longitude = s.Longitude,
        TemTomada = s.TemTomada, TemCalibrador = s.TemCalibrador, TemVestiario = s.TemVestiario,
        TemArmario = s.TemArmario, TemEspacoManutencao = s.TemEspacoManutencao,
        TemCadeado = s.TemCadeado, TemBanheiro = s.TemBanheiro,
        AcessoLivre = s.AcessoLivre, AcessoPago = s.AcessoPago,
        AcessoCadastro = s.AcessoCadastro, AcessoMensal = s.AcessoMensal,
        VeiculosSuportados = s.VeiculosSuportados,
        StatusAprovacao = s.StatusAprovacao,
        Deletado = s.Deletado,
        HorariosJson = s.HorariosJson,
    };

    private static BicicletarioDetalheDto MapearDetalhe(Bicicletario b) => new()
    {
        Id = b.Id, Nome = b.Nome, Latitude = b.Latitude, Longitude = b.Longitude,
        TemTomada = b.TemTomada, TemCalibrador = b.TemCalibrador, TemVestiario = b.TemVestiario,
        TemArmario = b.TemArmario, TemEspacoManutencao = b.TemEspacoManutencao,
        TemCadeado = b.TemCadeado, TemBanheiro = b.TemBanheiro,
        Horarios = b.Horarios.OrderBy(h => h.DiaSemana).Select(h => new HorarioFuncionamentoDto
        {
            DiaSemana = h.DiaSemana,
            HoraAbertura = h.HoraAbertura.ToString("HH:mm"),
            HoraFechamento = h.HoraFechamento.ToString("HH:mm"),
        }).ToList(),
        AcessoLivre = b.AcessoLivre, AcessoPago = b.AcessoPago,
        AcessoCadastro = b.AcessoCadastro, AcessoMensal = b.AcessoMensal,
        VeiculosSuportados = b.VeiculosSuportados,
        OperadorId = b.OperadorId,
        NomeOperador = b.Operador?.DisplayName,
        StatusAprovacao = b.StatusAprovacao,
        NomeCriador = b.Criador?.DisplayName,
        TierCriador = b.Criador?.Tier,
        NotaMedia = b.Avaliacoes.Any() ? b.Avaliacoes.Average(a => (double)a.Nota) : 0,
        Avaliacoes = b.Avaliacoes.OrderByDescending(a => a.CriadoEm).Select(a => new AvaliacaoDto
        {
            Id = a.Id,
            UsuarioId = a.UsuarioId,
            NomeUsuario = a.Usuario.DisplayName,
            FotoPerfilUrl = a.Usuario.FotoPerfilUrl,
            Nota = a.Nota,
            Comentario = a.Comentario,
            CriadoEm = a.CriadoEm
        }).ToList(),
        CriadoEm = b.CriadoEm,
        AtualizadoEm = b.AtualizadoEm,
        IsDeleted = b.Deletado,
    };

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

    private static TipoTier ComputeTier(int pontos) => pontos switch
    {
        >= 50 => TipoTier.Ouro,
        >= 10 => TipoTier.Prata,
        _     => TipoTier.Padrao,
    };
}
