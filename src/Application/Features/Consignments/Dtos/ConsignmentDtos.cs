using AutoGallerySaaS.Domain.Entities.Vehicles;

namespace AutoGallerySaaS.Application.Features.Consignments.Dtos;

public record BrokeredConsignmentDto(
    Guid Id,
    string OwnerName,
    string? OwnerPhone,
    string? CustomerName,
    string? CustomerPhone,
    string Plate,
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
    DateTime ConsignmentDate,
    decimal PurchasePrice,
    decimal? CommissionAmount,
    decimal? CommissionRate,
    ConsignmentStatus Status,
    string? Description
);

public record StockConsignmentDto(
    Guid Id,
    Guid? VehicleId,
    string OwnerName,
    string? OwnerPhone,
    string Plate,
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
    DateTime ConsignmentDate,
    decimal BasePrice,
    decimal? ExpectedSalePrice,
    decimal? CommissionAmount,
    decimal? CommissionRate,
    decimal? SalePrice,
    decimal? NetAmountToOwner,
    DateTime? SaleDate,
    ConsignmentStatus Status,
    string? Description
);

public record CreateBrokeredConsignmentRequest(
    string OwnerName,
    string? OwnerPhone,
    string? CustomerName,
    string? CustomerPhone,
    string Plate,
    Guid SegmentId,
    Guid BrandId,
    Guid ModelId,
    int Year,
    string Color,
    string EngineNumber,
    string ChassisNumber,
    DateTime ConsignmentDate,
    decimal PurchasePrice,
    decimal? CommissionAmount,
    decimal? CommissionRate,
    string? Description
);

public record UpdateBrokeredConsignmentRequest(
    string OwnerName,
    string? OwnerPhone,
    string? CustomerName,
    string? CustomerPhone,
    string Plate,
    Guid SegmentId,
    Guid BrandId,
    Guid ModelId,
    int Year,
    string Color,
    string EngineNumber,
    string ChassisNumber,
    DateTime ConsignmentDate,
    decimal PurchasePrice,
    decimal? CommissionAmount,
    decimal? CommissionRate,
    string? Description
);

public record CreateStockConsignmentRequest(
    string OwnerName,
    string? OwnerPhone,
    string Plate,
    Guid SegmentId,
    Guid BrandId,
    Guid ModelId,
    int Year,
    string Color,
    string EngineNumber,
    string ChassisNumber,
    DateTime ConsignmentDate,
    decimal BasePrice,
    decimal? ExpectedSalePrice,
    decimal? CommissionAmount,
    decimal? CommissionRate,
    string? Description
);

public record UpdateStockConsignmentRequest(
    string OwnerName,
    string? OwnerPhone,
    string Plate,
    Guid SegmentId,
    Guid BrandId,
    Guid ModelId,
    int Year,
    string Color,
    string EngineNumber,
    string ChassisNumber,
    DateTime ConsignmentDate,
    decimal BasePrice,
    decimal? ExpectedSalePrice,
    decimal? CommissionAmount,
    decimal? CommissionRate,
    string? Description
);

public record CompleteStockConsignmentSaleRequest(
    decimal SalePrice,
    DateTime SaleDate,
    decimal? CommissionAmount,
    decimal? CommissionRate,
    string? Description
);
