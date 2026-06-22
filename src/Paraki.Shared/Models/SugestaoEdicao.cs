using Paraki.Shared.Enums;

namespace Paraki.Shared.Models;

public class SugestaoEdicao
{
    public Guid Id { get; set; }

    public Guid BicicletarioId { get; set; }
    public Bicicletario Bicicletario { get; set; } = null!;

    public Guid AutorId { get; set; }
    public Usuario Autor { get; set; } = null!;

    public Guid? RevisorId { get; set; }
    public Usuario? Revisor { get; set; }

    public StatusSugestao Status { get; set; } = StatusSugestao.Pendente;

    // JSON serializado de AtualizarBicicletarioDto
    public string DadosEdicao { get; set; } = string.Empty;

    public string? MotivoRejeicao { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AvaliadaEm { get; set; }
}
