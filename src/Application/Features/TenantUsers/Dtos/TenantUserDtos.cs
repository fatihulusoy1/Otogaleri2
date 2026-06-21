namespace AutoGallerySaaS.Application.Features.TenantUsers.Dtos;

public record TenantUserDto(
    Guid Id,
    string FullName,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    bool IsTenantAdmin,
    bool IsSuperAdmin,
    DateTime? LastLoginAt
);

public record CreateTenantUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    bool IsTenantAdmin
);

public record UpdateTenantUserRequest(
    string FirstName,
    string LastName,
    string Email,
    bool IsTenantAdmin
);

public record SetTenantUserStatusRequest(
    bool IsActive
);

public record SetTenantUserPasswordRequest(
    string Password
);
