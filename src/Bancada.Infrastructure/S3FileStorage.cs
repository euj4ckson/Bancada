using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Bancada.Application;

namespace Bancada.Infrastructure;

public sealed class S3FileStorage : IFileStorage, IDisposable
{
    private readonly SupabaseStorageOptions _options;
    private readonly AmazonS3Client _client;

    public S3FileStorage(SupabaseStorageOptions options)
    {
        options.Validate();
        _options = options;
        var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
        _client = new AmazonS3Client(credentials, new AmazonS3Config
        {
            ServiceURL = options.Endpoint,
            AuthenticationRegion = options.Region,
            ForcePathStyle = true
        });
    }

    public async Task<string> SaveAsync(FileUpload file, string folder, CancellationToken cancellationToken = default)
    {
        FileStorageValidation.Validate(file);
        var objectKey = FileStorageValidation.CreateObjectKey(folder);
        await using var optimized = await ImageOptimizer.OptimizeAsync(file, cancellationToken);
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = optimized,
            ContentType = "image/webp",
            AutoCloseStream = false
        };

        await _client.PutObjectAsync(request, cancellationToken);
        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{objectKey}";
    }

    public async Task DeleteAsync(string url, CancellationToken cancellationToken = default)
    {
        var prefix = _options.PublicBaseUrl.TrimEnd('/') + "/";
        if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await _client.DeleteObjectAsync(_options.BucketName, url[prefix.Length..], cancellationToken);
    }

    public void Dispose() => _client.Dispose();
}
