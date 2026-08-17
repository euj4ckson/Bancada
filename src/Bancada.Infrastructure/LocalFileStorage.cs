using Bancada.Application;

namespace Bancada.Infrastructure;

public sealed class LocalFileStorage(string rootPath, string publicBasePath = "/uploads") : IFileStorage
{
    public async Task<string> SaveAsync(FileUpload file, string folder, CancellationToken cancellationToken = default)
    {
        var objectKey = FileStorageValidation.CreateObjectKey(file, folder);
        var destination = Path.Combine(rootPath, objectKey.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(destination) ?? throw new InvalidOperationException("Invalid upload path.");
        Directory.CreateDirectory(directory);

        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await file.Content.CopyToAsync(output, cancellationToken);

        return $"{publicBasePath.TrimEnd('/')}/{objectKey}";
    }

    public Task DeleteAsync(string url, CancellationToken cancellationToken = default)
    {
        var prefix = publicBasePath.TrimEnd('/') + "/";
        if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var relativePath = url[prefix.Length..].Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));
        var fullRoot = Path.GetFullPath(rootPath) + Path.DirectorySeparatorChar;
        if (fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
