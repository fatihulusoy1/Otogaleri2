using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.TenantActivities.Dtos;
using AutoGallerySaaS.Domain.Entities.SaaS;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.TenantActivities.Services;

public class TenantActivityService : ITenantActivityService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public TenantActivityService(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task LogAsync(Guid tenantId, TenantActivityType type, string description, decimal? amount = null)
    {
        var performedBy = await ResolvePerformedByAsync();

        _context.TenantActivities.Add(new TenantActivity
        {
            TenantId = tenantId,
            Type = type,
            Description = description,
            Amount = amount,
            PerformedBy = performedBy
        });

        await _context.SaveChangesAsync();
    }

    public async Task<List<TenantActivityDto>> GetActivitiesAsync()
    {
        var activities = await _context.TenantActivities
            .OrderByDescending(activity => activity.CreatedAt)
            .Take(200)
            .ToListAsync();

        return activities.Select(Map).ToList();
    }

    public async Task<List<TenantActivityDto>> GetActivitiesForTenantAsync(Guid tenantId)
    {
        var activities = await _context.TenantActivities
            .IgnoreQueryFilters()
            .Where(activity => activity.TenantId == tenantId)
            .OrderByDescending(activity => activity.CreatedAt)
            .Take(200)
            .ToListAsync();

        return activities.Select(Map).ToList();
    }

    private static TenantActivityDto Map(TenantActivity activity)
    {
        return new TenantActivityDto(
            activity.Id,
            (int)activity.Type,
            GetTypeLabel(activity.Type),
            activity.Description,
            activity.Amount,
            activity.PerformedBy,
            activity.CreatedAt);
    }

    private async Task<string?> ResolvePerformedByAsync()
    {
        if (!Guid.TryParse(_currentUser.UserId, out var userId))
        {
            return null;
        }

        return await _context.Users.IgnoreQueryFilters()
            .Where(user => user.Id == userId)
            .Select(user => $"{user.FirstName} {user.LastName}".Trim() == "" ? user.Email : $"{user.FirstName} {user.LastName}")
            .FirstOrDefaultAsync();
    }

    private static string GetTypeLabel(TenantActivityType type)
    {
        return type switch
        {
            TenantActivityType.SubscriptionPurchased => "Abonelik alındı",
            TenantActivityType.SubscriptionRenewed => "Abonelik yenilendi",
            TenantActivityType.PlanChanged => "Paket değiştirildi",
            TenantActivityType.UserCreated => "Kullanıcı oluşturuldu",
            TenantActivityType.UserStatusChanged => "Kullanıcı durumu değişti",
            TenantActivityType.UserDeleted => "Kullanıcı silindi",
            TenantActivityType.TenantAdminChanged => "Yönetici yetkisi değişti",
            TenantActivityType.UserLoggedIn => "Giriş yapıldı",
            TenantActivityType.UserLoginFailed => "Başarısız giriş denemesi",
            TenantActivityType.ProfileUpdated => "Profil güncellendi",
            TenantActivityType.PasswordChanged => "Şifre değiştirildi",
            TenantActivityType.PasswordReset => "Şifre sıfırlandı",
            TenantActivityType.TenantCreated => "Tenant oluşturuldu",
            TenantActivityType.TenantUpdated => "Tenant güncellendi",
            TenantActivityType.TenantStatusChanged => "Tenant durumu değişti",
            TenantActivityType.TenantDeleted => "Tenant silindi",
            _ => "İşlem"
        };
    }
}
