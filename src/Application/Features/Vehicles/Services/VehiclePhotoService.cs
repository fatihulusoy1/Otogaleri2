using AutoGallerySaaS.Application.Common.Exceptions;
using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Vehicles.Dtos;
using AutoGallerySaaS.Domain.Entities.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.Vehicles.Services;

public class VehiclePhotoService : IVehiclePhotoService
{
    // Uzun kenarin indirilecegi piksel siniri. Galeri goruntusu icin yeterli, depolama maliyetini dusurur.
    private const int MaxEdgePixels = 1600;

    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly IImageProcessor _imageProcessor;

    public VehiclePhotoService(
        IApplicationDbContext context,
        IFileStorageService storage,
        IImageProcessor imageProcessor)
    {
        _context = context;
        _storage = storage;
        _imageProcessor = imageProcessor;
    }

    public async Task<List<VehiclePhotoDto>> GetByVehicleAsync(Guid vehicleId)
    {
        return await _context.VehiclePhotos
            .Where(photo => photo.VehicleId == vehicleId)
            .OrderBy(photo => photo.SortOrder)
            .ThenBy(photo => photo.CreatedAt)
            .Select(photo => MapPhoto(photo))
            .ToListAsync();
    }

    public async Task<List<VehiclePhotoDto>> UploadAsync(Guid vehicleId, IReadOnlyList<PhotoUploadInput> files)
    {
        if (files.Count == 0)
        {
            throw new BusinessRuleException("Yuklenecek fotograf bulunamadi.");
        }

        var vehicleExists = await _context.Vehicles.AnyAsync(vehicle => vehicle.Id == vehicleId);
        if (!vehicleExists)
        {
            throw new BusinessRuleException("Arac bulunamadi.");
        }

        var nextSortOrder = await _context.VehiclePhotos
            .Where(photo => photo.VehicleId == vehicleId)
            .Select(photo => (int?)photo.SortOrder)
            .MaxAsync() ?? -1;
        nextSortOrder += 1;

        var created = new List<VehiclePhoto>();

        foreach (var file in files)
        {
            if (!IsImage(file.ContentType))
            {
                throw new BusinessRuleException("Sadece resim dosyalari yuklenebilir.");
            }

            var processed = await _imageProcessor.ProcessAsync(file.Content, MaxEdgePixels);
            await using var content = processed.Content;

            var storedFileName = BuildStoredFileName(file.FileName, processed.FileExtension);
            var result = await _storage.UploadAsync(content, storedFileName, processed.ContentType);

            var photo = new VehiclePhoto
            {
                VehicleId = vehicleId,
                FileName = result.FileName,
                FileUrl = result.FileUrl,
                FileSize = processed.Length,
                ContentType = processed.ContentType,
                SortOrder = nextSortOrder++
            };

            _context.VehiclePhotos.Add(photo);
            created.Add(photo);
        }

        await _context.SaveChangesAsync();

        return created.Select(MapPhoto).ToList();
    }

    public async Task DeleteAsync(Guid photoId)
    {
        var photo = await _context.VehiclePhotos.FirstOrDefaultAsync(item => item.Id == photoId);
        if (photo == null)
        {
            return;
        }

        photo.IsDeleted = true;
        await _context.SaveChangesAsync();

        // Soft-delete sonrasi fiziksel dosyayi da temizle (R2 implementasyonu objeyi siler).
        await _storage.DeleteAsync(photo.FileName);
    }

    public async Task<List<VehiclePhotoDto>> SetCoverAsync(Guid photoId)
    {
        var target = await _context.VehiclePhotos.FirstOrDefaultAsync(item => item.Id == photoId);
        if (target == null)
        {
            throw new BusinessRuleException("Fotograf bulunamadi.");
        }

        var photos = await _context.VehiclePhotos
            .Where(photo => photo.VehicleId == target.VehicleId)
            .OrderBy(photo => photo.SortOrder)
            .ThenBy(photo => photo.CreatedAt)
            .ToListAsync();

        // Secilen fotografi basa al; liste thumbnail'i her zaman ilk (SortOrder=0) fotografi gosterir.
        var ordered = photos
            .Where(photo => photo.Id == photoId)
            .Concat(photos.Where(photo => photo.Id != photoId))
            .ToList();

        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].SortOrder = index;
        }

        await _context.SaveChangesAsync();

        return ordered.Select(MapPhoto).ToList();
    }

    private static VehiclePhotoDto MapPhoto(VehiclePhoto photo) => new(
        photo.Id,
        photo.VehicleId,
        photo.FileName,
        photo.FileUrl,
        photo.FileSize,
        photo.ContentType,
        photo.SortOrder);

    private static bool IsImage(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType) &&
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    private static string BuildStoredFileName(string originalFileName, string newExtension)
    {
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "photo";
        }

        return $"{baseName}{newExtension}";
    }
}
