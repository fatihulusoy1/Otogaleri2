using AutoGallerySaaS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AutoGallerySaaS.Infrastructure.Services;

public class TenantService : ITenantService
{
    private Guid? _tenantId;

    public Guid? GetTenantId() => _tenantId;

    public void SetTenantId(Guid tenantId)
    {
        if (_tenantId.HasValue && _tenantId != tenantId)
        {
            throw new InvalidOperationException("TenantId is already set.");
        }
        _tenantId = tenantId;
    }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public Guid? TenantId
    {
        get
        {
            var tenantClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("tenant_id");
            return Guid.TryParse(tenantClaim, out var tenantId) ? tenantId : null;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool IsSuperAdmin
    {
        get
        {
            var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirstValue("is_super_admin");
            return bool.TryParse(claimValue, out var isSuperAdmin) && isSuperAdmin;
        }
    }
}

public class DateTimeService : IDateTime
{
    public DateTime Now => DateTime.UtcNow;
}
