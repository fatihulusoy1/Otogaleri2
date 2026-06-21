using AutoGallerySaaS.Domain.Entities.SaaS;

namespace AutoGallerySaaS.Application.Features.Subscription.Dtos;

public record SubscriptionPlanDto(
    Guid Id,
    string Name,
    string Description,
    decimal MonthlyPrice,
    decimal YearlyPrice,
    int MaxUsers,
    int MaxVehicles
);

public record SubscriptionStatusDto(
    Guid TenantId,
    string TenantName,
    Guid PlanId,
    string PlanName,
    decimal MonthlyPrice,
    decimal YearlyPrice,
    int MaxUsers,
    int MaxVehicles,
    int CurrentUsers,
    int CurrentVehicles,
    DateTime SubscriptionEndDate,
    int DaysRemaining,
    bool IsExpired
);

public record CheckoutRequest(
    Guid PlanId,
    BillingCycle BillingCycle
);
