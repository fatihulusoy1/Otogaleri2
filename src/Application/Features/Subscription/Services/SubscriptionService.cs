using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Subscription.Dtos;
using AutoGallerySaaS.Application.Features.TenantActivities.Services;
using AutoGallerySaaS.Domain.Entities.SaaS;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.Subscription.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantActivityService _activityLog;

    public SubscriptionService(IApplicationDbContext context, ICurrentUserService currentUser, ITenantActivityService activityLog)
    {
        _context = context;
        _currentUser = currentUser;
        _activityLog = activityLog;
    }

    public async Task<List<SubscriptionPlanDto>> GetPlansAsync()
    {
        return await _context.SubscriptionPlans
            .IgnoreQueryFilters()
            .Where(plan => plan.IsActive)
            .OrderBy(plan => plan.MonthlyPrice)
            .Select(plan => new SubscriptionPlanDto(
                plan.Id,
                plan.Name,
                plan.Description,
                plan.MonthlyPrice,
                plan.YearlyPrice,
                plan.MaxUsers,
                plan.MaxVehicles))
            .ToListAsync();
    }

    public async Task<SubscriptionStatusDto> GetCurrentAsync()
    {
        var tenantId = _currentUser.TenantId ?? throw new Exception("Aktif tenant bulunamadı.");
        return await BuildStatusAsync(tenantId);
    }

    public async Task<SubscriptionStatusDto> CheckoutAsync(CheckoutRequest request)
    {
        var tenantId = _currentUser.TenantId ?? throw new Exception("Aktif tenant bulunamadı.");

        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == tenantId && !item.IsDeleted);
        if (tenant == null)
        {
            throw new Exception("Tenant bulunamadı.");
        }

        var plan = await _context.SubscriptionPlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == request.PlanId && item.IsActive);
        if (plan == null)
        {
            throw new Exception("Seçilen paket bulunamadı.");
        }

        // Ödeme altyapısı henüz yok: başarılı ödeme varsayımıyla süreyi uzatıyoruz.
        var now = DateTime.UtcNow;
        var baseDate = tenant.SubscriptionEndDate > now ? tenant.SubscriptionEndDate : now;
        var planChanged = tenant.SubscriptionPlanId != plan.Id;

        tenant.SubscriptionPlanId = plan.Id;
        tenant.SubscriptionEndDate = request.BillingCycle == BillingCycle.Yearly
            ? baseDate.AddYears(1)
            : baseDate.AddMonths(1);

        // Plan içeriğinin anlık görüntüsünü (snapshot) bu döneme sabitle.
        // Plan sonradan değişse bile mevcut dönem bu limitlerle çalışır.
        tenant.EffectiveMaxUsers = plan.MaxUsers;
        tenant.EffectiveMaxVehicles = plan.MaxVehicles;

        await _context.SaveChangesAsync();

        var amount = request.BillingCycle == BillingCycle.Yearly ? plan.YearlyPrice : plan.MonthlyPrice;
        var cycleLabel = request.BillingCycle == BillingCycle.Yearly ? "Yıllık" : "Aylık";
        await _activityLog.LogAsync(
            tenantId,
            planChanged ? TenantActivityType.PlanChanged : TenantActivityType.SubscriptionRenewed,
            $"{plan.Name} paketi · {cycleLabel} · yeni bitiş: {tenant.SubscriptionEndDate:yyyy-MM-dd}",
            amount);

        return await BuildStatusAsync(tenantId);
    }

    private async Task<SubscriptionStatusDto> BuildStatusAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == tenantId);
        if (tenant == null)
        {
            throw new Exception("Tenant bulunamadı.");
        }

        var plan = await _context.SubscriptionPlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == tenant.SubscriptionPlanId);

        var currentUsers = await _context.Users.IgnoreQueryFilters()
            .CountAsync(item => item.TenantId == tenantId && !item.IsDeleted);
        var currentVehicles = await _context.Vehicles.IgnoreQueryFilters()
            .CountAsync(item => item.TenantId == tenantId && !item.IsDeleted);

        var now = DateTime.UtcNow;
        var daysRemaining = (int)Math.Ceiling((tenant.SubscriptionEndDate - now).TotalDays);
        var isExpired = tenant.SubscriptionEndDate < now;

        // Limitler, mevcut döneme sabitlenmiş snapshot'tan gelir (plan sonradan değişse de etkilenmez).
        var maxUsers = tenant.EffectiveMaxUsers > 0 ? tenant.EffectiveMaxUsers : (plan?.MaxUsers ?? 0);
        var maxVehicles = tenant.EffectiveMaxVehicles > 0 ? tenant.EffectiveMaxVehicles : (plan?.MaxVehicles ?? 0);

        return new SubscriptionStatusDto(
            tenant.Id,
            tenant.Name,
            tenant.SubscriptionPlanId,
            plan?.Name ?? "-",
            plan?.MonthlyPrice ?? 0,
            plan?.YearlyPrice ?? 0,
            maxUsers,
            maxVehicles,
            currentUsers,
            currentVehicles,
            tenant.SubscriptionEndDate,
            daysRemaining,
            isExpired);
    }
}
