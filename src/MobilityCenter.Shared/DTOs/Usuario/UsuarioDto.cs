using MobilityCenter.Shared.Enums;

namespace MobilityCenter.Shared.DTOs.Usuario;

public class UsuarioDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TipoUsuario Type { get; set; }
    public DateTime CreatedAt { get; set; }
}
