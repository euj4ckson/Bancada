namespace Bancada.Infrastructure;

public sealed class StorageOptions
{
    public string Provider { get; set; } = "Local";
    public string LocalPath { get; set; } = "wwwroot/uploads";
    public SupabaseStorageOptions Supabase { get; set; } = new();
}

public sealed class SupabaseStorageOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;

    public void Validate()
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Storage:Supabase:Endpoint must be an absolute HTTPS URL.");
        }

        if (!Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out var publicBaseUrl) || publicBaseUrl.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Storage:Supabase:PublicBaseUrl must be an absolute HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(Region) || string.IsNullOrWhiteSpace(AccessKeyId) ||
            string.IsNullOrWhiteSpace(SecretAccessKey) || string.IsNullOrWhiteSpace(BucketName))
        {
            throw new InvalidOperationException("The Supabase Storage region, credentials, and bucket name must be configured.");
        }
    }
}
