using Paraki.Shared.Enums;
using NetTopologySuite.Geometries;

namespace Paraki.Shared.Models;

public class Bicicletario
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public Point? Location { get; set; }

    // Serviços
    public bool TemTomada { get; set; }
    public bool TemCalibrador { get; set; }
    public bool TemVestiario { get; set; }
    public bool TemArmario { get; set; }
    public bool TemEspacoManutencao { get; set; }
    public bool TemCadeado { get; set; }
    public bool TemBanheiro { get; set; }

    // Acesso
    public bool AcessoLivre { get; set; }
    public bool AcessoPago { get; set; }
    public bool AcessoCadastro { get; set; }
    public bool AcessoMensal { get; set; }

    public TipoVeiculo VeiculosSuportados { get; set; } = TipoVeiculo.Nenhum;

    public Guid? OperadorId { get; set; }
    public Usuario? Operador { get; set; }

    public Guid? CriadorId { get; set; }
    public Usuario? Criador { get; set; }

    public StatusBicicletario StatusAprovacao { get; set; } = StatusBicicletario.Pendente;

    public ICollection<Avaliacao> Avaliacoes { get; set; } = [];
    public ICollection<SugestaoEdicao> Sugestoes { get; set; } = [];
    public ICollection<HorarioFuncionamento> Horarios { get; set; } = [];
    public ICollection<FotoBicicletario> Fotos { get; set; } = [];
    public ICollection<LogAuditoria> Logs { get; set; } = [];

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    public bool Deletado { get; set; }
}
