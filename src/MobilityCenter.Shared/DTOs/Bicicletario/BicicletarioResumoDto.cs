using MobilityCenter.Shared.Enums;

namespace MobilityCenter.Shared.DTOs.Bicicletario;

public class BicicletarioResumoDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double NotaMedia { get; set; }
    public int TotalAvaliacoes { get; set; }
    public TipoVeiculo VeiculosSuportados { get; set; }

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
    public bool IsDeleted { get; set; }
}
