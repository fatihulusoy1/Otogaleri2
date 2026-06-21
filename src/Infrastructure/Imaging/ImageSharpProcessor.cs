using AutoGallerySaaS.Application.Common.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace AutoGallerySaaS.Infrastructure.Imaging;

public class ImageSharpProcessor : IImageProcessor
{
    public async Task<ProcessedImage> ProcessAsync(Stream input, int maxEdge, CancellationToken cancellationToken = default)
    {
        using var image = await Image.LoadAsync(input, cancellationToken);

        // EXIF yonelimini uygula ve metadata'yi temizle (boyut + gizlilik).
        image.Mutate(context => context.AutoOrient());

        if (image.Width > maxEdge || image.Height > maxEdge)
        {
            image.Mutate(context => context.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxEdge, maxEdge)
            }));
        }

        image.Metadata.ExifProfile = null;

        var output = new MemoryStream();
        var encoder = new JpegEncoder { Quality = 80 };
        await image.SaveAsync(output, encoder, cancellationToken);
        output.Position = 0;

        return new ProcessedImage(output, "image/jpeg", ".jpg", output.Length);
    }
}
