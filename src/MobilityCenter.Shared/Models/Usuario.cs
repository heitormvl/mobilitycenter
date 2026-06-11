using Microsoft.AspNetCore.Identity;
using MobilityCenter.Shared.Enums;

namespace MobilityCenter.Shared.Models;

public class Usuario : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public TipoUsuario Type { get; set; } = TipoUsuario.Usuario;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? FotoPerfilUrl { get; set; }

    public ICollection<Bicicletario> Bicicletarios { get; set; } = [];
    public ICollection<Avaliacao> Reviews { get; set; } = [];
    public ICollection<SugestaoEdicao> SugestoesEnviadas { get; set; } = [];
}
