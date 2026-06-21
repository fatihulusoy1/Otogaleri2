using AutoGallerySaaS.Application.Features.Vehicles.Dtos;

namespace AutoGallerySaaS.Application.Features.Vehicles.Services;

public interface IVehiclePhotoService
{
    Task<List<VehiclePhotoDto>> GetByVehicleAsync(Guid vehicleId);
    Task<List<VehiclePhotoDto>> UploadAsync(Guid vehicleId, IReadOnlyList<PhotoUploadInput> files);
    Task DeleteAsync(Guid photoId);
    Task<List<VehiclePhotoDto>> SetCoverAsync(Guid photoId);
}
