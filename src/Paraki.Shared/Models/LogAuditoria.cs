using Paraki.Shared.Enums;

namespace Paraki.Shared.Models;

public class LogAuditoria
{
    public Guid Id { get; set; }
    public TipoAcaoAuditoria TipoAcao { get; set; }

    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public string NomeUsuario { get; set; } = string.Empty;

    public Guid BicicletarioId { get; set; }
    public Bicicletario Bicicletario { get; set; } = null!;

    public Guid? SugestaoId { get; set; }

    public Guid? SnapshotAntesId { get; set; }
    public SnapshotBicicletario? SnapshotAntes { get; set; }

    public Guid? SnapshotDepoisId { get; set; }
    public SnapshotBicicletario? SnapshotDepois { get; set; }

    public string? Observacao { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
