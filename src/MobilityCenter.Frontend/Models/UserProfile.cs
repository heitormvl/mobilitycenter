namespace MobilityCenter.Frontend.Models;

public class UserProfile
{
    public UserInfo Usuario { get; set; } = null!;
    public int TotalAvaliacoes { get; set; }
    public int TotalAdicionados { get; set; }
}
