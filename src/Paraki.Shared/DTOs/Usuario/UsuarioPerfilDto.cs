namespace Paraki.Shared.DTOs.Usuario;

public class UsuarioPerfilDto
{
    public UsuarioDto Usuario { get; set; } = null!;
    public int TotalAvaliacoes { get; set; }
    public int TotalAdicionados { get; set; }
}
