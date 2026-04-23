using System;

namespace AutoGallerySaaS.Application.Common.Interfaces;

public interface ITenantService
{
    Guid? GetTenantId();
    void SetTenantId(Guid tenantId);
}

public interface ICurrentUserService
{
    string? UserId { get; }
    Guid? TenantId { get; }
    bool IsAuthenticated { get; }
    bool IsSuperAdmin { get; }
}

public interface IDateTime
{
    DateTime Now { get; }
}
