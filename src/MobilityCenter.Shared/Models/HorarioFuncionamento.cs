namespace MobilityCenter.Shared.Models;

public class HorarioFuncionamento
{
    public Guid Id { get; set; }
    public Guid BicicletarioId { get; set; }
    public Bicicletario Bicicletario { get; set; } = null!;
    public DayOfWeek DiaSemana { get; set; }
    public TimeOnly HoraAbertura { get; set; }
    public TimeOnly HoraFechamento { get; set; }
}
