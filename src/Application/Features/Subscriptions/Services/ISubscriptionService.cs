namespace AutoGallerySaaS.Application.Features.Subscriptions.Services;

/// <summary>
/// Aktif tenant'in abonelik planina gore kaynak limitlerini denetler.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>Tenant yeni bir arac ekleyebilir mi? Limit dolu ise <see cref="Common.Exceptions.SubscriptionException"/> firlatir.</summary>
    Task EnsureCanAddVehicleAsync();

    /// <summary>Tenant yeni bir kullanici ekleyebilir mi? Limit dolu ise <see cref="Common.Exceptions.SubscriptionException"/> firlatir.</summary>
    Task EnsureCanAddUserAsync();
}
