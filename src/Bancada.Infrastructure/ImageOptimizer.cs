using Bancada.Application;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Bancada.Infrastructure;

internal static class ImageOptimizer
{
    private const int MaximumDimension = 1280;
    private const int FallbackMaximumDimension = 960;
    private const int MaximumPixelCount = 30_000_000;
    private const int MaximumOptimizedLength = 1024 * 1024;
    private const int WebpQuality = 76;

    public static async Task<MemoryStream> OptimizeAsync(FileUpload file, CancellationToken cancellationToken)
    {
        await using var input = new MemoryStream((int)file.Length);
        await file.Content.CopyToAsync(input, cancellationToken);
        input.Position = 0;

        try
        {
            var information = await Image.IdentifyAsync(input, cancellationToken);
            if ((long)information.Width * information.Height > MaximumPixelCount)
            {
                throw new InvalidOperationException("A resolução da imagem é muito alta.");
            }
        }
        catch (Exception exception) when (exception is UnknownImageFormatException or InvalidImageContentException)
        {
            throw new InvalidOperationException("O arquivo enviado não contém uma imagem válida.", exception);
        }

        input.Position = 0;
        Image image;
        try
        {
            image = await Image.LoadAsync(input, cancellationToken);
        }
        catch (Exception exception) when (exception is UnknownImageFormatException or InvalidImageContentException)
        {
            throw new InvalidOperationException("O arquivo enviado não contém uma imagem válida.", exception);
        }

        using (image)
        {
            ResizeIfNeeded(image, MaximumDimension);
            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;
            image.Metadata.XmpProfile = null;

            var output = new MemoryStream();
            await image.SaveAsWebpAsync(output, new WebpEncoder { Quality = WebpQuality }, cancellationToken);
            if (output.Length > MaximumOptimizedLength)
            {
                output.SetLength(0);
                ResizeIfNeeded(image, FallbackMaximumDimension);
                await image.SaveAsWebpAsync(output, new WebpEncoder { Quality = 62 }, cancellationToken);
            }

            if (output.Length > MaximumOptimizedLength)
            {
                await output.DisposeAsync();
                throw new InvalidOperationException("Não foi possível reduzir a imagem para menos de 1 MB.");
            }

            output.Position = 0;
            return output;
        }
    }

    private static void ResizeIfNeeded(Image image, int maximumDimension)
    {
        image.Mutate(context =>
        {
            context.AutoOrient();
            if (image.Width > maximumDimension || image.Height > maximumDimension)
            {
                context.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maximumDimension, maximumDimension)
                });
            }
        });
    }
}
