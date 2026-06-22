namespace Paraki.Frontend.Models;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public UserInfo Usuario { get; set; } = null!;
}
