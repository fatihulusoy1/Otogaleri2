using AutoGallerySaaS.Application.Features.TenantActivities.Dtos;
using AutoGallerySaaS.Domain.Entities.SaaS;

namespace AutoGallerySaaS.Application.Features.TenantActivities.Services;

public interface ITenantActivityService
{
    Task LogAsync(Guid tenantId, TenantActivityType type, string description, decimal? amount = null);
    Task<List<TenantActivityDto>> GetActivitiesAsync();
    Task<List<TenantActivityDto>> GetActivitiesForTenantAsync(Guid tenantId);
}
