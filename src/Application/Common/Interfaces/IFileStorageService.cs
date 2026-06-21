namespace AutoGallerySaaS.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Dosyayi depoya yazar ve geri donen anahtar (object key / dosya adi) ile public URL'i dondurur.
    /// Local'de wwwroot/uploads altina yazar; canlida R2 implementasyonu objeyi bucket'a koyar.
    /// </summary>
    Task<FileStorageResult> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);

    Task DeleteAsync(string fileName, CancellationToken cancellationToken = default);
}

public record FileStorageResult(string FileName, string FileUrl);
