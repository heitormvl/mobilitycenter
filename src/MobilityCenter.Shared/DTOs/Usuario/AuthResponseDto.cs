namespace MobilityCenter.Shared.DTOs.Usuario;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UsuarioDto Usuario { get; set; } = null!;
}
