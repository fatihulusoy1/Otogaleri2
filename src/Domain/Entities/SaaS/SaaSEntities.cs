using AutoGallerySaaS.Domain.Common;

namespace AutoGallerySaaS.Domain.Entities.SaaS;

public class Tenant : AuditableEntity, ISoftDelete
{
    public string Name { get; set; } = null!;
    public string? Identifier { get; set; } // For subdomains or slugs
    public string? ConnectionString { get; set; } // For future DB isolation
    public bool IsActive { get; set; } = true;
    public Guid SubscriptionPlanId { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;
    public DateTime SubscriptionEndDate { get; set; }
    public bool IsTrial { get; set; } // Ucretsiz deneme surecindeyse true; abonelik baslayinca false yapilir.

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public ICollection<TenantSetting> Settings { get; set; } = new List<TenantSetting>();
}

public class SubscriptionPlan : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public int MaxUsers { get; set; }
    public int MaxVehicles { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TenantSetting : BaseTenantEntity
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
}
