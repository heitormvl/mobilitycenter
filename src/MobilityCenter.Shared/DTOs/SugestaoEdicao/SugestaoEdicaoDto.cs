using MobilityCenter.Shared.DTOs.Bicicletario;
using MobilityCenter.Shared.Enums;

namespace MobilityCenter.Shared.DTOs.SugestaoEdicao;

public class SugestaoEdicaoDto
{
    public Guid Id { get; set; }
    public Guid BicicletarioId { get; set; }
    public string NomeBicicletario { get; set; } = string.Empty;
    public Guid AutorId { get; set; }
    public string NomeAutor { get; set; } = string.Empty;
    public Guid? RevisorId { get; set; }
    public string? NomeRevisor { get; set; }
    public StatusSugestao Status { get; set; }
    public AtualizarBicicletarioDto DadosEdicao { get; set; } = new();
    public string? MotivoRejeicao { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? AvaliadaEm { get; set; }
}
