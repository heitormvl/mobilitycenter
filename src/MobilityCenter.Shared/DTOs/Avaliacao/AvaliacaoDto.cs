namespace MobilityCenter.Shared.DTOs.Avaliacao;

public class AvaliacaoDto
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string NomeUsuario { get; set; } = string.Empty;
    public int Nota { get; set; }
    public string? Comentario { get; set; }
    public DateTime CriadoEm { get; set; }
}
