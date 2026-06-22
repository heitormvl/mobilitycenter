namespace Paraki.Frontend.Services;

public class AdicionarStateService
{
    public byte[]? FotoBytes { get; set; }
    public string? FotoContentType { get; set; }
    public bool HasFoto => FotoBytes is not null;

    public HorarioModel[] Horarios { get; set; } = [];

    public void ClearFoto()
    {
        FotoBytes = null;
        FotoContentType = null;
    }

    public void ClearHorarios() => Horarios = [];
}
