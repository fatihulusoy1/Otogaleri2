using AutoGallerySaaS.Application.Common.Exceptions;
using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Domain.Entities.SaaS;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.Subscriptions.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IApplicationDbContext _context;

    public SubscriptionService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task EnsureCanAddVehicleAsync()
    {
        var plan = await GetCurrentPlanAsync();
        if (plan is null)
        {
            return;
        }

        var vehicleCount = await _context.Vehicles.CountAsync();
        if (vehicleCount >= plan.MaxVehicles)
        {
            throw new SubscriptionException(
                $"Plan limitinize ulastiniz ({plan.MaxVehicles} arac). Daha fazla arac eklemek icin planinizi yukseltmelisiniz.");
        }
    }

    public async Task EnsureCanAddUserAsync()
    {
        var plan = await GetCurrentPlanAsync();
        if (plan is null)
        {
            return;
        }

        var userCount = await _context.Users.CountAsync();
        if (userCount >= plan.MaxUsers)
        {
            throw new SubscriptionException(
                $"Plan limitinize ulastiniz ({plan.MaxUsers} kullanici). Daha fazla kullanici eklemek icin planinizi yukseltmelisiniz.");
        }
    }

    /// <summary>
    /// Aktif tenant'in planini dondurur. Super admin veya tenant baglami yoksa null doner (limit uygulanmaz).
    /// </summary>
    private async Task<SubscriptionPlan?> GetCurrentPlanAsync()
    {
        if (_context.CurrentUserIsSuperAdmin)
        {
            return null;
        }

        var tenantId = _context.CurrentTenantId;
        if (tenantId is null || tenantId == Guid.Empty)
        {
            return null;
        }

        var planId = await _context.Tenants
            .Where(tenant => tenant.Id == tenantId)
            .Select(tenant => (Guid?)tenant.SubscriptionPlanId)
            .FirstOrDefaultAsync();

        if (planId is null || planId == Guid.Empty)
        {
            return null;
        }

        return await _context.SubscriptionPlans
            .FirstOrDefaultAsync(plan => plan.Id == planId);
    }
}
