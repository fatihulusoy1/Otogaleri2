using AutoGallerySaaS.Domain.Entities.Finance;

namespace AutoGallerySaaS.Application.Features.Admin.Dtos;

public record AdminTenantDto(
    Guid Id,
    string Name,
    string? Identifier,
    bool IsActive,
    int UserCount,
    Guid SubscriptionPlanId,
    string SubscriptionPlanName,
    DateTime SubscriptionEndDate
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

public record CreatePlanRequest(
    string Name,
    string Description,
    decimal MonthlyPrice,
    decimal YearlyPrice,
    int MaxUsers,
    int MaxVehicles,
    bool IsActive
);

public record UpdatePlanRequest(
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
    bool IsSuperAdmin,
    DateTime? LastLoginAt
);

public record CreateTenantRequest(
    string Name,
    string? Identifier,
    Guid SubscriptionPlanId,
    DateTime SubscriptionEndDate,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string AdminPassword
);

public record UpdateTenantRequest(
    string Name,
    string? Identifier,
    Guid SubscriptionPlanId,
    DateTime SubscriptionEndDate,
    bool IsActive
);

public record CreateUserRequest(
    Guid TenantId,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    bool IsSuperAdmin
);

public record UpdateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    bool IsSuperAdmin
);

public record SetUserPasswordRequest(
    string Password
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
