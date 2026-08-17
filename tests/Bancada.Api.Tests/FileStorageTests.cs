using System.Text;
using Bancada.Application;
using Bancada.Infrastructure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bancada.Api.Tests;

public sealed class FileStorageTests
{
    [Fact]
    public async Task Local_storage_converts_an_uploaded_image_to_webp()
    {
        var root = Path.Combine(Path.GetTempPath(), "bancada-storage-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var storage = new LocalFileStorage(root);
            await using var input = new MemoryStream();
            using (var image = new Image<Rgba32>(10, 10, Color.Orange))
            {
                await image.SaveAsPngAsync(input);
            }
            input.Position = 0;

            var url = await storage.SaveAsync(new FileUpload(input, "image/png", input.Length), "recipes");
            var path = Path.Combine(root, url["/uploads/".Length..].Replace('/', Path.DirectorySeparatorChar));
            var stored = await File.ReadAllBytesAsync(path);

            Assert.EndsWith(".webp", url, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("RIFF", Encoding.ASCII.GetString(stored, 0, 4));
            Assert.Equal("WEBP", Encoding.ASCII.GetString(stored, 8, 4));
            Assert.True(stored.Length <= 1024 * 1024);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Supabase_storage_rejects_incomplete_configuration()
    {
        var options = new SupabaseStorageOptions();

        var exception = Assert.Throws<InvalidOperationException>(() => new S3FileStorage(options));

        Assert.Contains("Endpoint", exception.Message);
    }
}
