namespace Paraki.Shared.DTOs.Admin;

public class PendenciasContagemDto
{
    public int Sugestoes { get; set; }
    public int Bicis { get; set; }
    public int Total => Sugestoes + Bicis;
}
