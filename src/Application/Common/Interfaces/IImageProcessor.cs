namespace AutoGallerySaaS.Application.Common.Interfaces;

public interface IImageProcessor
{
    /// <summary>
    /// Yuklenen goruntuyu yeniden boyutlandirip sikistirir (uzun kenar maxEdge'e indirilir, JPEG'e cevrilir).
    /// Depolama ve bant genisligi maliyetini dusurmek icindir.
    /// </summary>
    Task<ProcessedImage> ProcessAsync(Stream input, int maxEdge, CancellationToken cancellationToken = default);
}

public record ProcessedImage(Stream Content, string ContentType, string FileExtension, long Length);
