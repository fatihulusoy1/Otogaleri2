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

    // Abone olunan/yenilenen dönemde geçerli olan plan limitlerinin anlık görüntüsü (snapshot).
    // Plan içeriği sonradan değişse bile mevcut dönem bu değerlerle çalışır; yeni değerler bir sonraki yenilemede uygulanır.
    public int EffectiveMaxUsers { get; set; }
    public int EffectiveMaxVehicles { get; set; }

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

public enum BillingCycle
{
    Monthly = 1,
    Yearly = 2
}

public enum TenantActivityType
{
    SubscriptionPurchased = 1,
    SubscriptionRenewed = 2,
    PlanChanged = 3,
    UserCreated = 4,
    UserStatusChanged = 5,
    UserDeleted = 6,
    TenantAdminChanged = 7,
    UserLoggedIn = 8,
    UserLoginFailed = 9,
    ProfileUpdated = 10,
    PasswordChanged = 11,
    PasswordReset = 12,
    TenantCreated = 13,
    TenantUpdated = 14,
    TenantStatusChanged = 15,
    TenantDeleted = 16
}

public class TenantActivity : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public TenantActivityType Type { get; set; }
    public string Description { get; set; } = null!;
    public decimal? Amount { get; set; }
    public string? PerformedBy { get; set; }
}
