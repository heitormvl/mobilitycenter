using Paraki.Shared.DTOs.Avaliacao;
using Paraki.Shared.Enums;

namespace Paraki.Shared.DTOs.Bicicletario;

public class BicicletarioDetalheDto
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

    public ICollection<HorarioFuncionamentoDto> Horarios { get; set; } = [];

    public bool AcessoLivre { get; set; }
    public bool AcessoPago { get; set; }
    public bool AcessoCadastro { get; set; }
    public bool AcessoMensal { get; set; }

    public TipoVeiculo VeiculosSuportados { get; set; }

    public Guid? OperadorId { get; set; }
    public string? NomeOperador { get; set; }

    public double NotaMedia { get; set; }
    public ICollection<AvaliacaoDto> Avaliacoes { get; set; } = [];

    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
    public bool IsDeleted { get; set; }
    public StatusBicicletario StatusAprovacao { get; set; }
    public string? NomeCriador { get; set; }
    public TipoTier? TierCriador { get; set; }
}
