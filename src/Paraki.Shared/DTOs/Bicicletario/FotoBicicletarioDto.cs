namespace Paraki.Shared.DTOs.Bicicletario;

public class FotoBicicletarioDto
{
    public Guid Id { get; set; }
    public Guid BicicletarioId { get; set; }
    public string FotoUrl { get; set; } = "";
    public bool IsCapa { get; set; }
    public int Ordem { get; set; }
    public DateTime CriadoEm { get; set; }
}
