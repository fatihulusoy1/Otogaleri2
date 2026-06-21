using AutoGallerySaaS.Domain.Entities.Finance;

namespace AutoGallerySaaS.Application.Features.Admin.Dtos;

public record AdminTenantDto(
    Guid Id,
    string Name,
    string? Identifier,
    bool IsActive,
    int UserCount
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
