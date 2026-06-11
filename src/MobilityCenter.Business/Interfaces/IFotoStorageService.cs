namespace MobilityCenter.Business.Interfaces;

public interface IFotoStorageService
{
    Task<string> UploadFotoPerfilAsync(Guid usuarioId, Stream imageStream, string contentType);
    Task<(Stream stream, string contentType)?> DownloadFotoPerfilAsync(Guid usuarioId);
    Task<string> UploadFotoBicicletarioAsync(Guid bicicletarioId, Stream imageStream, string contentType);
    Task<(Stream stream, string contentType)?> DownloadFotoBicicletarioAsync(Guid bicicletarioId);
}
