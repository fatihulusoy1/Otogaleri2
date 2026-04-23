using AutoGallerySaaS.Domain.Entities.Finance;
using AutoGallerySaaS.Domain.Entities.Vehicles;

namespace AutoGallerySaaS.Application.Features.Vehicles.Dtos;

public record VehicleDto(
    Guid Id,
    string Plate,
    string Brand,
    string Model,
    int Year,
    string Color,
    decimal PurchasePrice,
    decimal TotalExpenseCost,
    decimal TotalCost,
    decimal? TargetSalePrice,
    decimal? ActualSalePrice,
    decimal? EstimatedProfit,
    VehicleStatus Status,
    string? Description
);

public record PurchaseRecordDto(
    Guid VehicleId,
    string Plate,
    string Brand,
    string Model,
    decimal PurchasePrice,
    DateTime PurchasedAt,
    VehicleStatus Status
);

public record VehicleSaleDto(
    Guid VehicleId,
    string Plate,
    decimal PurchasePrice,
    decimal TotalExpenseCost,
    decimal TotalCost,
    decimal SalePrice,
    decimal Profit,
    decimal ProfitMargin,
    DateTime SoldAt
);

public record VehicleExpenseDto(
    Guid Id,
    Guid VehicleId,
    string VehiclePlate,
    string Description,
    decimal Amount,
    DateTime ExpenseDate,
    PaymentMethod PaymentMethod
);

public record CreateVehicleRequest(
    string Plate,
    string Brand,
    string Model,
    int Year,
    string Color,
    string EngineNumber,
    string ChassisNumber,
    decimal PurchasePrice,
    decimal? TargetSalePrice,
    string? Description
);

public record CreatePurchaseRequest(
    string Plate,
    string Brand,
    string Model,
    int Year,
    string Color,
    string EngineNumber,
    string ChassisNumber,
    decimal PurchasePrice,
    decimal? TargetSalePrice,
    string? Description
);

public record UpdateVehicleRequest(
    string Plate,
    string Brand,
    string Model,
    int Year,
    string Color,
    decimal PurchasePrice,
    decimal? TargetSalePrice,
    VehicleStatus Status,
    string? Description
);

public record CreateVehicleExpenseRequest(
    string Description,
    decimal Amount,
    DateTime ExpenseDate,
    PaymentMethod PaymentMethod
);

public record CompleteVehicleSaleRequest(
    decimal SalePrice,
    DateTime SaleDate,
    PaymentMethod PaymentMethod,
    string? Description
);
