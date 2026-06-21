namespace AutoGallerySaaS.Application.Features.Subscription.Services;

public interface ISubscriptionLimitService
{
    Task EnsureCanAddUserAsync(Guid tenantId);
    Task EnsureCanAddVehicleAsync(Guid tenantId);
}
