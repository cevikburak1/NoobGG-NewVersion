namespace NoobGg.Application.Common.Helpers;

public static class ImageValidator
{
    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] WebPRiff = [0x52, 0x49, 0x46, 0x46];
    private static readonly byte[] WebPTag = [0x57, 0x45, 0x42, 0x50];

    public static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public static bool IsValidImageStream(Stream stream)
    {
        if (stream.Length < 12)
            return false;

        var header = new byte[12];
        var originalPos = stream.Position;
        stream.Position = 0;
        _ = stream.Read(header, 0, 12);
        stream.Position = originalPos;

        if (header[..3].SequenceEqual(JpegMagic))
            return true;

        if (header[..4].SequenceEqual(PngMagic))
            return true;

        if (header[..4].SequenceEqual(WebPRiff) && header[8..12].SequenceEqual(WebPTag))
            return true;

        return false;
    }

    public static string GetExtensionFromStream(Stream stream)
    {
        var header = new byte[12];
        var originalPos = stream.Position;
        stream.Position = 0;
        _ = stream.Read(header, 0, 12);
        stream.Position = originalPos;

        if (header[..3].SequenceEqual(JpegMagic))
            return ".jpg";

        if (header[..4].SequenceEqual(PngMagic))
            return ".png";

        if (header[..4].SequenceEqual(WebPRiff) && header[8..12].SequenceEqual(WebPTag))
            return ".webp";

        return ".bin";
    }
}
