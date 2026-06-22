using ImageMagick;

namespace MobilityCenter.Business.Services;

internal static class HeicConverter
{
    private static readonly HashSet<string> HeicTypes =
        ["image/heic", "image/heif", "image/heic-sequence", "image/heif-sequence"];

    /// <summary>
    /// If the image is HEIC/HEIF, converts to PNG bytes that SkiaSharp can decode.
    /// Otherwise returns the original bytes unchanged.
    /// </summary>
    public static byte[] EnsureSkiaDecodable(byte[] inputBytes, string contentType)
    {
        if (!HeicTypes.Contains(contentType.ToLowerInvariant()))
            return inputBytes;

        using var image = new MagickImage(inputBytes);
        image.Format = MagickFormat.Png;
        return image.ToByteArray();
    }
}
