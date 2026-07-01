namespace Paraki.RazorLib.Services;

public class FotoParaUpload
{
    public byte[] Bytes { get; set; } = [];
    public string ContentType { get; set; } = "image/jpeg";
}

public class AdicionarStateService
{
    public List<FotoParaUpload> Fotos { get; set; } = [];
    public bool HasFoto => Fotos.Count > 0;

    public HorarioModel[] Horarios { get; set; } = [];

    public void ClearFoto() => Fotos.Clear();
    public void ClearHorarios() => Horarios = [];
}
