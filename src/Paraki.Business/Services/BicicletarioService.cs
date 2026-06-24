using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Paraki.Business.Filters;
using Paraki.Business.Interfaces;
using Paraki.Repositories.Context;
using Paraki.Shared.DTOs.Avaliacao;
using Paraki.Shared.DTOs.Bicicletario;
using Paraki.Shared.DTOs.SugestaoEdicao;
using Paraki.Shared.Enums;
using Paraki.Shared.Exceptions;
using Paraki.Shared.Models;
using NetTopologySuite.Geometries;

namespace Paraki.Business.Services;

public class BicicletarioService : IBicicletarioService
{
    private readonly ParakiDbContext _db;

    public BicicletarioService(ParakiDbContext db) => _db = db;

    public async Task<IEnumerable<BicicletarioResumoDto>> ListarAsync(BicicletarioFiltros filtros)
    {
        var query = filtros.IncluirOcultas
            ? _db.Bicicletarios.IgnoreQueryFilters().AsQueryable()
            : _db.Bicicletarios.AsQueryable();

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
                // ST_DWithin com geometry SRID 4326 usa graus; ~1 grau = 111 km
                var raioGraus = filtros.RaioKm.Value / 111.0;
                query = query.Where(b => b.Location != null && b.Location.IsWithinDistance(ponto, raioGraus));
            }
        }

        var pontoCapturado = ponto;
        query = filtros.OrderBy switch
        {
            "nota" => query.OrderByDescending(b =>
                b.Avaliacoes.Select(a => (double?)a.Nota).Average() ?? 0),
            "avaliacoes" => query.OrderByDescending(b => b.Avaliacoes.Count),
            _ when pontoCapturado != null => query.OrderBy(b =>
                b.Location == null ? double.MaxValue : b.Location.Distance(pontoCapturado)),
            _ => query.OrderBy(b => b.Nome)
        };

        // Project to SQL to compute NotaMedia/TotalAvaliacoes as aggregates (no N+1).
        // Horarios are selected as raw TimeOnly values and formatted in memory.
        var rows = await query
            .Skip((filtros.Page - 1) * filtros.PageSize)
            .Take(filtros.PageSize)
            .Select(b => new
            {
                b.Id,
                b.Nome,
                b.Latitude,
                b.Longitude,
                NotaMedia = b.Avaliacoes.Average(a => (double?)a.Nota) ?? 0.0,
                TotalAvaliacoes = b.Avaliacoes.Count(),
                b.VeiculosSuportados,
                b.TemTomada,
                b.TemCalibrador,
                b.TemVestiario,
                b.TemArmario,
                b.TemEspacoManutencao,
                b.TemCadeado,
                b.TemBanheiro,
                b.AcessoLivre,
                b.AcessoPago,
                b.AcessoCadastro,
                b.AcessoMensal,
                IsDeleted = b.Deletado,
                Horarios = b.Horarios
                    .OrderBy(h => h.DiaSemana)
                    .Select(h => new { h.DiaSemana, h.HoraAbertura, h.HoraFechamento })
                    .ToList(),
                CapaId = b.Fotos.Where(f => f.IsCapa).Select(f => (Guid?)f.Id).FirstOrDefault()
            })
            .ToListAsync();

        return rows.Select(b => new BicicletarioResumoDto
        {
            Id = b.Id,
            Nome = b.Nome,
            Latitude = b.Latitude,
            Longitude = b.Longitude,
            NotaMedia = b.NotaMedia,
            TotalAvaliacoes = b.TotalAvaliacoes,
            VeiculosSuportados = b.VeiculosSuportados,
            TemTomada = b.TemTomada,
            TemCalibrador = b.TemCalibrador,
            TemVestiario = b.TemVestiario,
            TemArmario = b.TemArmario,
            TemEspacoManutencao = b.TemEspacoManutencao,
            TemCadeado = b.TemCadeado,
            TemBanheiro = b.TemBanheiro,
            AcessoLivre = b.AcessoLivre,
            AcessoPago = b.AcessoPago,
            AcessoCadastro = b.AcessoCadastro,
            AcessoMensal = b.AcessoMensal,
            IsDeleted = b.IsDeleted,
            Horarios = b.Horarios.Select(h => new HorarioFuncionamentoDto
            {
                DiaSemana = h.DiaSemana,
                HoraAbertura = h.HoraAbertura.ToString("HH:mm"),
                HoraFechamento = h.HoraFechamento.ToString("HH:mm")
            }).ToList(),
            CapaUrl = b.CapaId.HasValue
                ? $"/api/fotos/bicicletario/{b.Id}/{b.CapaId.Value}"
                : null
        });
    }

    public async Task<BicicletarioDetalheDto> ObterPorIdAsync(Guid id)
    {
        var b = await _db.Bicicletarios
            .IgnoreQueryFilters()
            .Include(b => b.Avaliacoes).ThenInclude(a => a.Usuario)
            .Include(b => b.Operador)
            .Include(b => b.Horarios)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException($"Bicicletário {id} não encontrado.");

        return MapearDetalhe(b);
    }

    public async Task<BicicletarioDetalheDto> CriarAsync(CriarBicicletarioDto dto, Guid usuarioId)
    {
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
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
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

        await _db.SaveChangesAsync();

        return await ObterPorIdAsync(bicicletario.Id);
    }

    public async Task<ResultadoAtualizacaoDto> AtualizarAsync(Guid id, AtualizarBicicletarioDto dto, Guid usuarioId)
    {
        var bicicletario = await _db.Bicicletarios.FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException($"Bicicletário {id} não encontrado.");

        var usuario = await _db.Users.FindAsync(usuarioId)
            ?? throw new NotFoundException("Usuário não encontrado.");

        bool ehDono = bicicletario.OperadorId == usuarioId;
        bool podeEditarDireto = usuario.Type == TipoUsuario.Admin
            || (usuario.Type == TipoUsuario.Operador && ehDono);

        if (podeEditarDireto)
        {
            if (dto.Nome != null) bicicletario.Nome = dto.Nome;
            if (dto.Latitude.HasValue) bicicletario.Latitude = dto.Latitude.Value;
            if (dto.Longitude.HasValue) bicicletario.Longitude = dto.Longitude.Value;
            if (dto.Latitude.HasValue || dto.Longitude.HasValue)
                bicicletario.Location = new Point(bicicletario.Longitude, bicicletario.Latitude) { SRID = 4326 };

            if (dto.TemTomada.HasValue) bicicletario.TemTomada = dto.TemTomada.Value;
            if (dto.TemCalibrador.HasValue) bicicletario.TemCalibrador = dto.TemCalibrador.Value;
            if (dto.TemVestiario.HasValue) bicicletario.TemVestiario = dto.TemVestiario.Value;
            if (dto.TemArmario.HasValue) bicicletario.TemArmario = dto.TemArmario.Value;
            if (dto.TemEspacoManutencao.HasValue) bicicletario.TemEspacoManutencao = dto.TemEspacoManutencao.Value;
            if (dto.TemCadeado.HasValue) bicicletario.TemCadeado = dto.TemCadeado.Value;
            if (dto.TemBanheiro.HasValue) bicicletario.TemBanheiro = dto.TemBanheiro.Value;

            if (dto.Horarios != null)
            {
                var existentes = await _db.HorariosFuncionamento
                    .Where(h => h.BicicletarioId == id)
                    .ToListAsync();
                _db.HorariosFuncionamento.RemoveRange(existentes);
                foreach (var h in dto.Horarios)
                    _db.HorariosFuncionamento.Add(new HorarioFuncionamento
                    {
                        Id = Guid.NewGuid(),
                        BicicletarioId = id,
                        DiaSemana = h.DiaSemana,
                        HoraAbertura = TimeOnly.Parse(h.HoraAbertura, System.Globalization.CultureInfo.InvariantCulture),
                        HoraFechamento = TimeOnly.Parse(h.HoraFechamento, System.Globalization.CultureInfo.InvariantCulture),
                    });
            }

            if (dto.AcessoLivre.HasValue) bicicletario.AcessoLivre = dto.AcessoLivre.Value;
            if (dto.AcessoPago.HasValue) bicicletario.AcessoPago = dto.AcessoPago.Value;
            if (dto.AcessoCadastro.HasValue) bicicletario.AcessoCadastro = dto.AcessoCadastro.Value;
            if (dto.AcessoMensal.HasValue) bicicletario.AcessoMensal = dto.AcessoMensal.Value;

            if (dto.VeiculosSuportados.HasValue) bicicletario.VeiculosSuportados = dto.VeiculosSuportados.Value;

            bicicletario.AtualizadoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new ResultadoAtualizacaoDto
            {
                EditadoDireto = true,
                Bicicletario = await ObterPorIdAsync(id)
            };
        }

        // Usuário comum ou Operador sem dono: cria sugestão na fila
        var sugestao = new SugestaoEdicao
        {
            Id = Guid.NewGuid(),
            BicicletarioId = id,
            AutorId = usuarioId,
            DadosEdicao = JsonSerializer.Serialize(dto),
            CriadoEm = DateTime.UtcNow
        };

        _db.SugestoesEdicao.Add(sugestao);
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
                CriadoEm = sugestao.CriadoEm
            }
        };
    }

    public async Task DeletarAsync(Guid id, Guid usuarioId, TipoUsuario tipoUsuario)
    {
        var bicicletario = await _db.Bicicletarios.FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException($"Bicicletário {id} não encontrado.");

        if (tipoUsuario != TipoUsuario.Admin && bicicletario.OperadorId != usuarioId)
            throw new UnauthorizedException("Sem permissão para deletar este bicicletário.");

        bicicletario.Deletado = true;
        bicicletario.AtualizadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task RestaurarAsync(Guid id, TipoUsuario tipoUsuario)
    {
        if (tipoUsuario != TipoUsuario.Admin)
            throw new UnauthorizedException("Apenas administradores podem restaurar bicicletários.");

        var bicicletario = await _db.Bicicletarios.IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException($"Bicicletário {id} não encontrado.");

        bicicletario.Deletado = false;
        bicicletario.AtualizadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task DeletarPermanenteAsync(Guid id, Guid usuarioId, TipoUsuario tipoUsuario)
    {
        if (tipoUsuario != TipoUsuario.Admin)
            throw new UnauthorizedException("Apenas administradores podem excluir permanentemente.");

        var bicicletario = await _db.Bicicletarios.FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException($"Bicicletário {id} não encontrado.");

        _db.Bicicletarios.Remove(bicicletario);
        await _db.SaveChangesAsync();
    }

    private static BicicletarioDetalheDto MapearDetalhe(Bicicletario b) => new()
    {
        Id = b.Id,
        Nome = b.Nome,
        Latitude = b.Latitude,
        Longitude = b.Longitude,
        TemTomada = b.TemTomada,
        TemCalibrador = b.TemCalibrador,
        TemVestiario = b.TemVestiario,
        TemArmario = b.TemArmario,
        TemEspacoManutencao = b.TemEspacoManutencao,
        TemCadeado = b.TemCadeado,
        TemBanheiro = b.TemBanheiro,
        Horarios = b.Horarios.OrderBy(h => h.DiaSemana).Select(h => new HorarioFuncionamentoDto
        {
            DiaSemana = h.DiaSemana,
            HoraAbertura = h.HoraAbertura.ToString("HH:mm"),
            HoraFechamento = h.HoraFechamento.ToString("HH:mm"),
        }).ToList(),
        AcessoLivre = b.AcessoLivre,
        AcessoPago = b.AcessoPago,
        AcessoCadastro = b.AcessoCadastro,
        AcessoMensal = b.AcessoMensal,
        VeiculosSuportados = b.VeiculosSuportados,
        OperadorId = b.OperadorId,
        NomeOperador = b.Operador?.DisplayName,
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
        IsDeleted = b.Deletado
    };
}
