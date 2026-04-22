using AutoGallerySaaS.Application.Features.Auth.Dtos;
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
    decimal? TargetSalePrice,
    VehicleStatus Status,
    string? Description
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
    decimal? TargetSalePrice
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
