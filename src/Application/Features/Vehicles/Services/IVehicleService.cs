using AutoGallerySaaS.Application.Features.Vehicles.Dtos;

namespace AutoGallerySaaS.Application.Features.Vehicles.Services;

public interface IVehicleService
{
    Task<List<VehicleDto>> GetAllAsync();
    Task<VehicleDto?> GetByIdAsync(Guid id);
    Task<VehicleDto> CreateAsync(CreateVehicleRequest request);
    Task<PurchaseRecordDto> CreatePurchaseAsync(CreatePurchaseRequest request);
    Task<List<PurchaseRecordDto>> GetPurchasesAsync();
    Task<VehicleExpenseDto> AddExpenseAsync(Guid vehicleId, CreateVehicleExpenseRequest request);
    Task<List<VehicleExpenseDto>> GetExpensesAsync(Guid? vehicleId = null);
    Task<VehicleSaleDto> CompleteSaleAsync(Guid vehicleId, CompleteVehicleSaleRequest request);
    Task<List<VehicleSaleDto>> GetSalesAsync();
    Task UpdateAsync(Guid id, UpdateVehicleRequest request);
    Task DeleteAsync(Guid id);
}
