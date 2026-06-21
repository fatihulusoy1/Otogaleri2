namespace AutoGallerySaaS.Application.Features.TenantActivities.Dtos;

public record TenantActivityDto(
    Guid Id,
    int Type,
    string TypeLabel,
    string Description,
    decimal? Amount,
    string? PerformedBy,
    DateTime CreatedAt
);
