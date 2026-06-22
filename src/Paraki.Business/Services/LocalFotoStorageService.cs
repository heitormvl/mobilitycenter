using Paraki.Business.Interfaces;
using SkiaSharp;

namespace Paraki.Business.Services;

/// <summary>
/// Implementação local para desenvolvimento — salva fotos no sistema de arquivos.
/// Configure AzureStorage:StorageType = "Azure" para usar o Azure Blob Storage em produção.
/// </summary>
public class LocalFotoStorageService : IFotoStorageService
{
    private readonly string _basePath;
    private const int MaxDimension = 500;

    public LocalFotoStorageService(string basePath)
    {
        _basePath = basePath;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadFotoPerfilAsync(Guid usuarioId, Stream imageStream, string contentType)
    {
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        var inputBytes = HeicConverter.EnsureSkiaDecodable(ms.ToArray(), contentType);

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

            var filePath = Path.Combine(_basePath, $"{usuarioId}.webp");
            await File.WriteAllBytesAsync(filePath, encoded.ToArray());

            return $"/api/fotos/{usuarioId}";
        }
        finally
        {
            resized?.Dispose();
        }
    }

    public Task<(Stream stream, string contentType)?> DownloadFotoPerfilAsync(Guid usuarioId)
    {
        var filePath = Path.Combine(_basePath, $"{usuarioId}.webp");

        if (!File.Exists(filePath))
            return Task.FromResult<(Stream, string)?>(null);

        Stream stream = File.OpenRead(filePath);
        return Task.FromResult<(Stream, string)?>((stream, "image/webp"));
    }

    public async Task UploadFotoBicicletarioAsync(Guid bicicletarioId, Guid fotoId, Stream imageStream, string contentType)
    {
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        var inputBytes = HeicConverter.EnsureSkiaDecodable(ms.ToArray(), contentType);

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

            var dir = Path.Combine(_basePath, "bicicletarios", bicicletarioId.ToString());
            Directory.CreateDirectory(dir);
            await File.WriteAllBytesAsync(Path.Combine(dir, $"{fotoId}.webp"), encoded.ToArray());
        }
        finally
        {
            resized?.Dispose();
        }
    }

    public Task<(Stream stream, string contentType)?> DownloadFotoBicicletarioAsync(Guid bicicletarioId, Guid fotoId)
    {
        var filePath = Path.Combine(_basePath, "bicicletarios", bicicletarioId.ToString(), $"{fotoId}.webp");

        if (!File.Exists(filePath))
            return Task.FromResult<(Stream, string)?>(null);

        Stream stream = File.OpenRead(filePath);
        return Task.FromResult<(Stream, string)?>((stream, "image/webp"));
    }

    public Task<(Stream stream, string contentType)?> DownloadFotoBicicletarioLegacyAsync(Guid bicicletarioId)
    {
        var filePath = Path.Combine(_basePath, "bicicletarios", $"{bicicletarioId}.webp");

        if (!File.Exists(filePath))
            return Task.FromResult<(Stream, string)?>(null);

        Stream stream = File.OpenRead(filePath);
        return Task.FromResult<(Stream, string)?>((stream, "image/webp"));
    }

    public Task DeleteFotoBicicletarioAsync(Guid bicicletarioId, Guid fotoId)
    {
        var filePath = Path.Combine(_basePath, "bicicletarios", bicicletarioId.ToString(), $"{fotoId}.webp");

        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }

    public Task DeleteFotoPerfilAsync(Guid usuarioId)
    {
        var filePath = Path.Combine(_basePath, $"{usuarioId}.webp");

        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }
}
