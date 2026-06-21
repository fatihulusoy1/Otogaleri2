using AutoGallerySaaS.Domain.Entities.Finance;
using AutoGallerySaaS.Domain.Entities.Vehicles;

namespace AutoGallerySaaS.Application.Features.Vehicles.Dtos;

public record VehicleDto(
    Guid Id,
    string Plate,
    VehicleOwnershipType OwnershipType,
    Guid? ConsignmentId,
    Guid? SegmentId,
    string? Segment,
    Guid? BrandId,
    string Brand,
    Guid? ModelId,
    string Model,
    int Year,
    string Color,
    string EngineNumber,
    string ChassisNumber,
    DateTime PurchaseDate,
    decimal PurchasePrice,
    decimal TotalExpenseCost,
    decimal TotalCost,
    decimal? TargetSalePrice,
    decimal? ActualSalePrice,
    DateTime? SaleDate,
    decimal? ConsignmentCommissionAmount,
    decimal? EstimatedProfit,
    VehicleStatus Status,
    string? Description,
    IReadOnlyList<VehiclePhotoDto> Photos
);

public record VehiclePhotoDto(
    Guid Id,
    Guid VehicleId,
    string FileName,
    string FileUrl,
    long FileSize,
    string ContentType,
    int SortOrder
);

public record PhotoUploadInput(
    Stream Content,
    string FileName,
    string ContentType
);

public record PurchaseRecordDto(
    Guid VehicleId,
    string Plate,
    VehicleOwnershipType OwnershipType,
    Guid? ConsignmentId,
    Guid? SegmentId,
    string? Segment,
    Guid? BrandId,
    string Brand,
    Guid? ModelId,
    string Model,
    int Year,
    string Color,
    string EngineNumber,
    string ChassisNumber,
    decimal PurchasePrice,
    PaymentMethod PaymentMethod,
    string? NotaryRegistryNumber,
    string? CounterpartyName,
    DateTime? DueDate,
    string? DocumentNumber,
    decimal? InstallmentAmount,
    int? InstallmentCount,
    int? InstallmentIntervalMonths,
    string? TradePlate,
    decimal? TradeAmount,
    decimal? TargetSalePrice,
    DateTime PurchasedAt,
    string? Description,
    VehicleStatus Status
);

public record LookupOptionDto(
    Guid Id,
    string Name
);

public record VehicleModelLookupDto(
    Guid Id,
    string Name,
    Guid BrandId,
    Guid? SegmentId
);

public record VehicleLookupsDto(
    List<LookupOptionDto> Segments,
    List<LookupOptionDto> Brands,
    List<VehicleModelLookupDto> Models
);

public record VehicleSaleDto(
    Guid VehicleId,
    string Plate,
    string VehicleDisplayName,
    decimal PurchasePrice,
    decimal TotalExpenseCost,
    decimal TotalCost,
    decimal SalePrice,
    decimal Profit,
    decimal ProfitMargin,
    DateTime SoldAt,
    PaymentMethod PaymentMethod,
    string? CounterpartyName,
    DateTime? DueDate,
    string? DocumentNumber,
    decimal? InstallmentAmount,
    int? InstallmentCount,
    int? InstallmentIntervalMonths,
    string? TradePlate,
    decimal? TradeAmount,
    string? NotaryRegistryNumber
);

public record VehicleExpenseDto(
    Guid Id,
    Guid VehicleId,
    string VehiclePlate,
    string Description,
    decimal Amount,
    DateTime ExpenseDate,
    Guid? CategoryId,
    string? CategoryName,
    PaymentMethod PaymentMethod
);

public record CreateVehicleRequest(
    string Plate,
    Guid SegmentId,
    Guid BrandId,
    Guid ModelId,
    int Year,
    string Color,
    string EngineNumber,
    string ChassisNumber,
    DateTime PurchaseDate,
    decimal PurchasePrice,
    decimal? TargetSalePrice,
    string? Description
);

public record CreatePurchaseRequest(
    string Plate,
    Guid SegmentId,
    Guid BrandId,
    Guid ModelId,
    int Year,
    string Color,
    string EngineNumber,
    string ChassisNumber,
    DateTime PurchaseDate,
    decimal PurchasePrice,
    PaymentMethod PaymentMethod,
    string? NotaryRegistryNumber,
    string? CounterpartyName,
    DateTime? DueDate,
    string? DocumentNumber,
    int? InstallmentCount,
    int? InstallmentIntervalMonths,
    string? TradePlate,
    decimal? TradeAmount,
    decimal? TargetSalePrice,
    string? Description
);

public record UpdatePurchaseRequest(
    string Plate,
    Guid SegmentId,
    Guid BrandId,
    Guid ModelId,
    int Year,
    string Color,
    string EngineNumber,
    string ChassisNumber,
    DateTime PurchaseDate,
    decimal PurchasePrice,
    PaymentMethod PaymentMethod,
    string? NotaryRegistryNumber,
    string? CounterpartyName,
    DateTime? DueDate,
    string? DocumentNumber,
    int? InstallmentCount,
    int? InstallmentIntervalMonths,
    string? TradePlate,
    decimal? TradeAmount,
    decimal? TargetSalePrice,
    string? Description
);

public record UpdateVehicleRequest(
    string Plate,
    Guid SegmentId,
    Guid BrandId,
    Guid ModelId,
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
    Guid? CategoryId,
    PaymentMethod PaymentMethod
);

public record UpdateVehicleExpenseRequest(
    string Description,
    decimal Amount,
    DateTime ExpenseDate,
    Guid? CategoryId,
    PaymentMethod PaymentMethod
);

public record CompleteVehicleSaleRequest(
    decimal SalePrice,
    DateTime SaleDate,
    PaymentMethod PaymentMethod,
    string? NotaryRegistryNumber,
    string? CounterpartyName,
    DateTime? DueDate,
    string? DocumentNumber,
    int? InstallmentCount,
    int? InstallmentIntervalMonths,
    string? TradePlate,
    decimal? TradeAmount,
    string? Description
);

public record UpdateVehicleSaleRequest(
    decimal SalePrice,
    DateTime SaleDate,
    PaymentMethod PaymentMethod,
    string? NotaryRegistryNumber,
    string? CounterpartyName,
    DateTime? DueDate,
    string? DocumentNumber,
    int? InstallmentCount,
    int? InstallmentIntervalMonths,
    string? TradePlate,
    decimal? TradeAmount,
    string? Description
);
