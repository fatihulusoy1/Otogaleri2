using AutoGallerySaaS.Application.Features.Subscription.Dtos;

namespace AutoGallerySaaS.Application.Features.Subscription.Services;

public interface ISubscriptionService
{
    Task<List<SubscriptionPlanDto>> GetPlansAsync();
    Task<SubscriptionStatusDto> GetCurrentAsync();
    Task<SubscriptionStatusDto> CheckoutAsync(CheckoutRequest request);
}
