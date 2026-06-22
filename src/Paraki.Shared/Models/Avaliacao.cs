namespace Paraki.Shared.Models;

public class Avaliacao
{
    public Guid Id { get; set; }

    public Guid BicicletarioId { get; set; }
    public Bicicletario Bicicletario { get; set; } = null!;

    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int Nota { get; set; }
    public string? Comentario { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
