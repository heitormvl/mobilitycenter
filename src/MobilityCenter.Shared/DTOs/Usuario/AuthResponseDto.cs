namespace MobilityCenter.Shared.DTOs.Usuario;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public UsuarioDto Usuario { get; set; } = null!;
}
