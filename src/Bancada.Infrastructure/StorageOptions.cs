namespace Bancada.Infrastructure;

public sealed class StorageOptions
{
    public string Provider { get; set; } = "Local";
    public string LocalPath { get; set; } = "wwwroot/uploads";
    public R2Options R2 { get; set; } = new();
}

public sealed class R2Options
{
    public string AccountId { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
}
