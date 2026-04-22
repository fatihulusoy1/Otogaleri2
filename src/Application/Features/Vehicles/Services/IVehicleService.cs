using AutoGallerySaaS.Application.Features.Auth.Dtos;
using AutoGallerySaaS.Application.Features.Vehicles.Dtos;

namespace AutoGallerySaaS.Application.Features.Vehicles.Services;

public interface IVehicleService
{
    Task<List<VehicleDto>> GetAllAsync();
    Task<VehicleDto?> GetByIdAsync(Guid id);
    Task<VehicleDto> CreateAsync(CreateVehicleRequest request);
    Task UpdateAsync(Guid id, UpdateVehicleRequest request);
    Task DeleteAsync(Guid id);
}
