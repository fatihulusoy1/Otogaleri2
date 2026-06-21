using AutoGallerySaaS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AutoGallerySaaS.Infrastructure.Storage;

/// <summary>
/// Fotograflari uygulama sunucusunda wwwroot/uploads altina yazar ve link uzerinden erisilebilir kilar.
/// Canliya gecince ayni arabirimi uygulayan bir R2/S3 servisi DI'da bunun yerine kayit edilir;
/// veritabaninda tutulan FileUrl link mantigi degismeden calismaya devam eder.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private const string UploadsFolder = "uploads";
    private readonly string _storagePath;
    // Public link tabani. Bos ise "/uploads/.." goreli yol uretilir (ayni origin / vite proxy).
    // R2 oncesi gecis icin "Storage:PublicBaseUrl" set edilirse mutlak URL uretir.
    private readonly string _publicBaseUrl;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", UploadsFolder);
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }

        _publicBaseUrl = (configuration["Storage:PublicBaseUrl"] ?? string.Empty).TrimEnd('/');
    }

    public async Task<FileStorageResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var uniqueFileName = $"{Guid.NewGuid():N}_{SanitizeFileName(fileName)}";
        var filePath = Path.Combine(_storagePath, uniqueFileName);

        await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await fileStream.CopyToAsync(stream, cancellationToken);
        }

        var fileUrl = string.IsNullOrEmpty(_publicBaseUrl)
            ? $"/{UploadsFolder}/{uniqueFileName}"
            : $"{_publicBaseUrl}/{uniqueFileName}";

        return new FileStorageResult(uniqueFileName, fileUrl);
    }

    public Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_storagePath, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        return string.IsNullOrWhiteSpace(name) ? "file" : name;
    }
}
