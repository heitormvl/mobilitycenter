using Paraki.Shared.Enums;

namespace Paraki.Shared.DTOs.Usuario;

public class CriarUsuarioDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public TipoUsuario Type { get; set; } = TipoUsuario.Usuario;
}
