namespace MobilityCenter.Shared.DTOs.Avaliacao;

public class CriarAvaliacaoDto
{
    public Guid BicicletarioId { get; set; }
    public int Nota { get; set; }
    public string? Comentario { get; set; }
}
