using Microsoft.AspNetCore.Identity;
using Paraki.Shared.Enums;

namespace Paraki.Shared.Models;

public class Usuario : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public TipoUsuario Type { get; set; } = TipoUsuario.Usuario;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? FotoPerfilUrl { get; set; }

    public int PontosAprovados { get; set; }
    public TipoTier? TierOverride { get; set; }

    public TipoTier Tier => TierOverride ?? PontosAprovados switch
    {
        >= 50 => TipoTier.Ouro,
        >= 10 => TipoTier.Prata,
        _     => TipoTier.Padrao,
    };

    public ICollection<Bicicletario> Bicicletarios { get; set; } = [];
    public ICollection<Avaliacao> Reviews { get; set; } = [];
    public ICollection<SugestaoEdicao> SugestoesEnviadas { get; set; } = [];
    public ICollection<LogAuditoria> LogsAuditoria { get; set; } = [];
}
