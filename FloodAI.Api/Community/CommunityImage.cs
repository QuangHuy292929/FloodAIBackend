namespace FloodAI.Api.Community;

internal static class CommunityImage
{
    internal const long MaxFileBytes = 5 * 1024 * 1024;
    internal const long MaxRequestBytes = MaxFileBytes + 64 * 1024;

    internal static async Task<string?> ReadMediaTypeAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length < 12 || file.Length > MaxFileBytes)
        {
            return null;
        }

        var declared = Path.GetExtension(file.FileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };
        if (declared is null || !string.Equals(declared, file.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var header = new byte[12];
        await using var input = file.OpenReadStream();
        var read = await input.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        if (read != header.Length)
        {
            return null;
        }

        var valid = declared switch
        {
            "image/jpeg" => header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff,
            "image/png" => header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            "image/webp" => header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                            header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
        return valid ? declared : null;
    }
}
