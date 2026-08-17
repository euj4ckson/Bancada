using Bancada.Application;

namespace Bancada.Infrastructure;

internal static class FileStorageValidation
{
    private const long MaximumLength = 5 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> Extensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    public static string CreateObjectKey(FileUpload file, string folder)
    {
        if (file.Length is <= 0 or > MaximumLength)
        {
            throw new InvalidOperationException("A imagem deve ter no máximo 5 MB.");
        }

        if (!Extensions.TryGetValue(file.ContentType, out var extension))
        {
            throw new InvalidOperationException("Envie uma imagem JPG, PNG ou WebP.");
        }

        if (folder.Length is < 1 or > 40 || folder.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            throw new ArgumentException("Invalid storage folder.", nameof(folder));
        }

        return $"{folder}/{Guid.NewGuid():N}{extension}";
    }
}
