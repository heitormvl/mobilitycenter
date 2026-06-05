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
}
