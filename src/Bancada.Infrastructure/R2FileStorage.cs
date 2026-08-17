using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Bancada.Application;

namespace Bancada.Infrastructure;

public sealed class R2FileStorage : IFileStorage, IDisposable
{
    private readonly R2Options _options;
    private readonly AmazonS3Client _client;

    public R2FileStorage(R2Options options)
    {
        _options = options;
        var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
        _client = new AmazonS3Client(credentials, new AmazonS3Config
        {
            ServiceURL = $"https://{options.AccountId}.r2.cloudflarestorage.com",
            AuthenticationRegion = "auto",
            ForcePathStyle = true
        });
    }

    public async Task<string> SaveAsync(FileUpload file, string folder, CancellationToken cancellationToken = default)
    {
        var objectKey = FileStorageValidation.CreateObjectKey(file, folder);
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = file.Content,
            ContentType = file.ContentType,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true
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
