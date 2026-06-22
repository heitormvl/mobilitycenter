namespace Paraki.Shared.Models;

public class FotoBicicletario
{
    public Guid Id { get; set; }
    public Guid BicicletarioId { get; set; }
    public Bicicletario? Bicicletario { get; set; }
    public string BlobKey { get; set; } = "";
    public bool IsCapa { get; set; }
    public int Ordem { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
