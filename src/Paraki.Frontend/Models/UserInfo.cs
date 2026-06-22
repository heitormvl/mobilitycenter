namespace Paraki.Frontend.Models;

public class UserInfo
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? FotoPerfilUrl { get; set; }
}
