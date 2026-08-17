using Bancada.Application;

namespace Bancada.Infrastructure;

internal static class FileStorageValidation
{
    private const long MaximumLength = 5 * 1024 * 1024;

    private static readonly IReadOnlySet<string> ContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/webp" };

    public static void Validate(FileUpload file)
    {
        if (file.Length is <= 0 or > MaximumLength)
        {
            throw new InvalidOperationException("A imagem deve ter no máximo 5 MB.");
        }

        if (!ContentTypes.Contains(file.ContentType))
        {
            throw new InvalidOperationException("Envie uma imagem JPG, PNG ou WebP.");
        }
    }

    public static string CreateObjectKey(string folder)
    {
        if (folder.Length is < 1 or > 40 || folder.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            throw new ArgumentException("Invalid storage folder.", nameof(folder));
        }

        return $"{folder}/{Guid.NewGuid():N}.webp";
    }
}
