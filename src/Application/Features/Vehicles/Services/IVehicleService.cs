using AutoGallerySaaS.Application.Features.Vehicles.Dtos;

namespace AutoGallerySaaS.Application.Features.Vehicles.Services;

public interface IVehicleService
{
    Task<VehicleLookupsDto> GetLookupsAsync();
    Task<List<VehicleDto>> GetAllAsync();
    Task<VehicleDto?> GetByIdAsync(Guid id);
    Task<VehicleDto> CreateAsync(CreateVehicleRequest request);
    Task<PurchaseRecordDto> CreatePurchaseAsync(CreatePurchaseRequest request);
    Task<PurchaseRecordDto> UpdatePurchaseAsync(Guid vehicleId, UpdatePurchaseRequest request);
    Task DeletePurchaseAsync(Guid vehicleId);
    Task<List<PurchaseRecordDto>> GetPurchasesAsync();
    Task<VehicleExpenseDto> AddExpenseAsync(Guid vehicleId, CreateVehicleExpenseRequest request);
    Task<VehicleExpenseDto> UpdateExpenseAsync(Guid expenseId, UpdateVehicleExpenseRequest request);
    Task DeleteExpenseAsync(Guid expenseId);
    Task<List<VehicleExpenseDto>> GetExpensesAsync(Guid? vehicleId = null);
    Task<VehicleSaleDto> CompleteSaleAsync(Guid vehicleId, CompleteVehicleSaleRequest request);
    Task<VehicleSaleDto> UpdateSaleAsync(Guid vehicleId, UpdateVehicleSaleRequest request);
    Task DeleteSaleAsync(Guid vehicleId);
    Task<List<VehicleSaleDto>> GetSalesAsync();
    Task UpdateAsync(Guid id, UpdateVehicleRequest request);
    Task DeleteAsync(Guid id);
}
