using AutoGallerySaaS.Domain.Entities.Finance;

namespace AutoGallerySaaS.Application.Features.Admin.Dtos;

public record AdminTenantDto(
    Guid Id,
    string Name,
    string? Identifier,
    bool IsActive,
    int UserCount,
    int VehicleCount,
    Guid SubscriptionPlanId,
    string? SubscriptionPlanName,
    DateTime SubscriptionEndDate,
    bool IsTrial,
    DateTime CreatedAt
);

public record AdminSubscriptionPlanDto(
    Guid Id,
    string Name,
    string Description,
    decimal MonthlyPrice,
    decimal YearlyPrice,
    int MaxUsers,
    int MaxVehicles,
    bool IsActive
);

public record UpdateTenantSubscriptionRequest(
    Guid SubscriptionPlanId,
    DateTime SubscriptionEndDate,
    bool IsTrial
);

public record SaveSubscriptionPlanRequest(
    string Name,
    string Description,
    decimal MonthlyPrice,
    decimal YearlyPrice,
    int MaxUsers,
    int MaxVehicles,
    bool IsActive
);

public record AdminUserDto(
    Guid Id,
    Guid TenantId,
    string TenantName,
    string FullName,
    string Email,
    bool IsActive,
    bool IsSuperAdmin
);

public record AdminCatalogLookupsDto(
    List<AdminLookupDto> Segments,
    List<AdminLookupDto> Brands,
    List<AdminCatalogModelDto> Models
);

public record AdminLookupDto(
    Guid Id,
    string Name
);

public record AdminCatalogModelDto(
    Guid Id,
    string Name,
    Guid BrandId,
    Guid? SegmentId
);

public record CreateAdminSegmentRequest(
    string Name
);

public record CreateAdminBrandRequest(
    string Name
);

public record CreateAdminModelRequest(
    Guid BrandId,
    Guid? SegmentId,
    string Name
);

public record CreateExpenseCategoryRequest(
    string Name,
    ExpenseCategoryType CategoryType
);

public record UpdateStatusRequest(
    bool IsActive
);

public record UpdateNameRequest(
    string Name
);
