using AutoGallerySaaS.Application.Common.Exceptions;
using AutoGallerySaaS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.Subscription.Services;

public class SubscriptionLimitService : ISubscriptionLimitService
{
    private readonly IApplicationDbContext _context;

    public SubscriptionLimitService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task EnsureCanAddUserAsync(Guid tenantId)
    {
        var limit = await ResolveMaxUsersAsync(tenantId);
        if (limit <= 0)
        {
            return;
        }

        var current = await _context.Users.IgnoreQueryFilters()
            .CountAsync(user => user.TenantId == tenantId && !user.IsDeleted);

        if (current >= limit)
        {
            throw new SubscriptionLimitException(
                $"Kullanıcı limitiniz dolu ({current}/{limit}). Daha fazla kullanıcı eklemek için paketinizi yükseltin.");
        }
    }

    public async Task EnsureCanAddVehicleAsync(Guid tenantId)
    {
        var limit = await ResolveMaxVehiclesAsync(tenantId);
        if (limit <= 0)
        {
            return;
        }

        var current = await _context.Vehicles.IgnoreQueryFilters()
            .CountAsync(vehicle => vehicle.TenantId == tenantId && !vehicle.IsDeleted);

        if (current >= limit)
        {
            throw new SubscriptionLimitException(
                $"Araç limitiniz dolu ({current}/{limit}). Daha fazla araç eklemek için paketinizi yükseltin.");
        }
    }

    private async Task<int> ResolveMaxUsersAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .Where(item => item.Id == tenantId)
            .Select(item => new { item.EffectiveMaxUsers, item.SubscriptionPlanId })
            .FirstOrDefaultAsync();

        if (tenant == null)
        {
            return 0;
        }

        if (tenant.EffectiveMaxUsers > 0)
        {
            return tenant.EffectiveMaxUsers;
        }

        // Snapshot yoksa (eski kayıt) plana göre düş.
        return await _context.SubscriptionPlans.IgnoreQueryFilters()
            .Where(plan => plan.Id == tenant.SubscriptionPlanId)
            .Select(plan => plan.MaxUsers)
            .FirstOrDefaultAsync();
    }

    private async Task<int> ResolveMaxVehiclesAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .Where(item => item.Id == tenantId)
            .Select(item => new { item.EffectiveMaxVehicles, item.SubscriptionPlanId })
            .FirstOrDefaultAsync();

        if (tenant == null)
        {
            return 0;
        }

        if (tenant.EffectiveMaxVehicles > 0)
        {
            return tenant.EffectiveMaxVehicles;
        }

        return await _context.SubscriptionPlans.IgnoreQueryFilters()
            .Where(plan => plan.Id == tenant.SubscriptionPlanId)
            .Select(plan => plan.MaxVehicles)
            .FirstOrDefaultAsync();
    }
}
