namespace MobilityCenter.Frontend.Models;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserInfo Usuario { get; set; } = null!;
}
