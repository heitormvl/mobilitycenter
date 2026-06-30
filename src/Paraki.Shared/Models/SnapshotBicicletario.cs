namespace Paraki.Shared.Models;

public class SnapshotBicicletario
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public bool TemTomada { get; set; }
    public bool TemCalibrador { get; set; }
    public bool TemVestiario { get; set; }
    public bool TemArmario { get; set; }
    public bool TemEspacoManutencao { get; set; }
    public bool TemCadeado { get; set; }
    public bool TemBanheiro { get; set; }

    public bool AcessoLivre { get; set; }
    public bool AcessoPago { get; set; }
    public bool AcessoCadastro { get; set; }
    public bool AcessoMensal { get; set; }

    public int VeiculosSuportados { get; set; }
    public int StatusAprovacao { get; set; }
    public bool Deletado { get; set; }

    public string HorariosJson { get; set; } = "[]";

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
