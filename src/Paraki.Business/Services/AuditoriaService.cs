using System.Text.Json;
using Paraki.Business.Interfaces;
using Paraki.Repositories.Context;
using Paraki.Shared.Enums;
using Paraki.Shared.Models;

namespace Paraki.Business.Services;

public class AuditoriaService : IAuditoriaService
{
    private readonly ParakiDbContext _db;

    public AuditoriaService(ParakiDbContext db) => _db = db;

    public SnapshotBicicletario CriarSnapshot(Bicicletario b)
    {
        var horariosJson = b.Horarios.Any()
            ? JsonSerializer.Serialize(b.Horarios.OrderBy(h => h.DiaSemana).Select(h => new
            {
                DiaSemana    = (int)h.DiaSemana,
                HoraAbertura = h.HoraAbertura.ToString("HH:mm"),
                HoraFechamento = h.HoraFechamento.ToString("HH:mm"),
            }))
            : "[]";

        return new SnapshotBicicletario
        {
            Id = Guid.NewGuid(),
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
            AcessoLivre = b.AcessoLivre,
            AcessoPago = b.AcessoPago,
            AcessoCadastro = b.AcessoCadastro,
            AcessoMensal = b.AcessoMensal,
            VeiculosSuportados = (int)b.VeiculosSuportados,
            StatusAprovacao = (int)b.StatusAprovacao,
            Deletado = b.Deletado,
            HorariosJson = horariosJson,
            CriadoEm = DateTime.UtcNow,
        };
    }

    public async Task RegistrarAsync(
        TipoAcaoAuditoria tipoAcao,
        Guid usuarioId,
        string nomeUsuario,
        Guid bicicletarioId,
        SnapshotBicicletario? antes,
        SnapshotBicicletario? depois,
        string? observacao = null,
        Guid? sugestaoId = null)
    {
        if (antes != null)
            _db.SnapshotsBicicletario.Add(antes);

        if (depois != null)
            _db.SnapshotsBicicletario.Add(depois);

        _db.LogsAuditoria.Add(new LogAuditoria
        {
            Id = Guid.NewGuid(),
            TipoAcao = tipoAcao,
            UsuarioId = usuarioId,
            NomeUsuario = nomeUsuario,
            BicicletarioId = bicicletarioId,
            SugestaoId = sugestaoId,
            SnapshotAntes = antes,
            SnapshotDepois = depois,
            Observacao = observacao,
            CriadoEm = DateTime.UtcNow,
        });
    }
}
