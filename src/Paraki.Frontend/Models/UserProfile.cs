namespace Paraki.Frontend.Models;

public class UserProfile
{
    public UserInfo? Usuario { get; set; }
    public int TotalAvaliacoes { get; set; }
    public int TotalAdicionados { get; set; }
    public int PontosAprovados { get; set; }
    public int Tier { get; set; }
}
