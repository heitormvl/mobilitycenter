namespace Paraki.Shared.DTOs.Bicicletario;

public class HorarioFuncionamentoDto
{
    public DayOfWeek DiaSemana { get; set; }
    public string HoraAbertura { get; set; } = "";
    public string HoraFechamento { get; set; } = "";
}
