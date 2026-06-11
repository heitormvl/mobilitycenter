using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MobilityCenter.Business.Interfaces;
using SkiaSharp;

namespace MobilityCenter.Business.Services;

public class FotoStorageService : IFotoStorageService
{
    private readonly BlobServiceClient _blobService;
    private const string ContainerName = "fotos-perfil";
    private const int MaxDimension = 500;

    public FotoStorageService(BlobServiceClient blobService)
    {
        _blobService = blobService;
    }

    public async Task<string> UploadFotoPerfilAsync(Guid usuarioId, Stream imageStream, string contentType)
    {
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        var inputBytes = ms.ToArray();

        using var bitmap = SKBitmap.Decode(inputBytes)
            ?? throw new InvalidOperationException("Não foi possível decodificar a imagem.");

        SKBitmap? resized = null;
        try
        {
            if (bitmap.Width > MaxDimension || bitmap.Height > MaxDimension)
            {
                var scale = Math.Min((float)MaxDimension / bitmap.Width, (float)MaxDimension / bitmap.Height);
                var newWidth = (int)(bitmap.Width * scale);
                var newHeight = (int)(bitmap.Height * scale);
                resized = bitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
            }

            var source = resized ?? bitmap;
            using var skImage = SKImage.FromBitmap(source);
            using var encoded = skImage.Encode(SKEncodedImageFormat.Webp, 85);
            using var output = new MemoryStream(encoded.ToArray());

            var container = _blobService.GetBlobContainerClient(ContainerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.None);

            var blob = container.GetBlobClient($"{usuarioId}.webp");
            await blob.UploadAsync(output, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "image/webp" }
            });

            // Foto servida via endpoint proxy da API para evitar exposição direta do blob
            return $"/api/fotos/{usuarioId}";
        }
        finally
        {
            resized?.Dispose();
        }
    }

    public async Task<(Stream stream, string contentType)?> DownloadFotoPerfilAsync(Guid usuarioId)
    {
        var container = _blobService.GetBlobContainerClient(ContainerName);
        var blob = container.GetBlobClient($"{usuarioId}.webp");

        if (!await blob.ExistsAsync())
            return null;

        var download = await blob.DownloadStreamingAsync();
        return (download.Value.Content, "image/webp");
    }

    public async Task<string> UploadFotoBicicletarioAsync(Guid bicicletarioId, Stream imageStream, string contentType)
    {
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        var inputBytes = ms.ToArray();

        using var bitmap = SKBitmap.Decode(inputBytes)
            ?? throw new InvalidOperationException("Não foi possível decodificar a imagem.");

        SKBitmap? resized = null;
        try
        {
            const int maxDim = 1200;
            if (bitmap.Width > maxDim || bitmap.Height > maxDim)
            {
                var scale = Math.Min((float)maxDim / bitmap.Width, (float)maxDim / bitmap.Height);
                resized = bitmap.Resize(new SKImageInfo((int)(bitmap.Width * scale), (int)(bitmap.Height * scale)), SKFilterQuality.High);
            }

            var source = resized ?? bitmap;
            using var skImage = SKImage.FromBitmap(source);
            using var encoded = skImage.Encode(SKEncodedImageFormat.Webp, 85);
            using var output = new MemoryStream(encoded.ToArray());

            const string bicicletariosContainer = "fotos-bicicletarios";
            var container = _blobService.GetBlobContainerClient(bicicletariosContainer);
            await container.CreateIfNotExistsAsync(PublicAccessType.None);

            var blob = container.GetBlobClient($"{bicicletarioId}.webp");
            await blob.UploadAsync(output, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "image/webp" }
            });

            return $"/api/fotos/bicicletario/{bicicletarioId}";
        }
        finally
        {
            resized?.Dispose();
        }
    }

    public async Task<(Stream stream, string contentType)?> DownloadFotoBicicletarioAsync(Guid bicicletarioId)
    {
        const string bicicletariosContainer = "fotos-bicicletarios";
        var container = _blobService.GetBlobContainerClient(bicicletariosContainer);
        var blob = container.GetBlobClient($"{bicicletarioId}.webp");

        if (!await blob.ExistsAsync())
            return null;

        var download = await blob.DownloadStreamingAsync();
        return (download.Value.Content, "image/webp");
    }
}
