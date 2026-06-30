using Paraki.Shared.Enums;

namespace Paraki.Shared.DTOs.Bicicletario;

public class BicicletarioPendenteDto
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

    public TipoVeiculo VeiculosSuportados { get; set; }

    public StatusBicicletario StatusAprovacao { get; set; }

    public Guid? CriadorId { get; set; }
    public string NomeCriador { get; set; } = string.Empty;
    public TipoTier TierCriador { get; set; }

    public List<HorarioFuncionamentoDto> Horarios { get; set; } = [];
    public string? CapaUrl { get; set; }

    public DateTime CriadoEm { get; set; }
}
