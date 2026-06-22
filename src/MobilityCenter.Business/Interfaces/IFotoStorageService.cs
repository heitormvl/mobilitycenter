namespace MobilityCenter.Business.Interfaces;

public interface IFotoStorageService
{
    Task<string> UploadFotoPerfilAsync(Guid usuarioId, Stream imageStream, string contentType);
    Task<(Stream stream, string contentType)?> DownloadFotoPerfilAsync(Guid usuarioId);

    Task UploadFotoBicicletarioAsync(Guid bicicletarioId, Guid fotoId, Stream imageStream, string contentType);
    Task<(Stream stream, string contentType)?> DownloadFotoBicicletarioAsync(Guid bicicletarioId, Guid fotoId);
    Task<(Stream stream, string contentType)?> DownloadFotoBicicletarioLegacyAsync(Guid bicicletarioId);
    Task DeleteFotoBicicletarioAsync(Guid bicicletarioId, Guid fotoId);
    Task DeleteFotoPerfilAsync(Guid usuarioId);
}
