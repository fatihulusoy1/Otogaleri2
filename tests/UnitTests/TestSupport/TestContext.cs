using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.UnitTests.TestSupport;

/// <summary>
/// EF Core InMemory saglayicisi ile gercek <see cref="ApplicationDbContext"/> ureten test yardimcisi.
/// Her cagri izole bir veritabani adi kullanir, boylece testler birbirini etkilemez.
/// </summary>
public static class TestContext
{
    public static ApplicationDbContext Create(
        out FakeTenantService tenantService,
        out FakeCurrentUserService currentUserService,
        Guid? tenantId = null,
        bool isSuperAdmin = false)
    {
        var resolvedTenantId = tenantId ?? Guid.NewGuid();

        tenantService = new FakeTenantService(resolvedTenantId);
        currentUserService = new FakeCurrentUserService(resolvedTenantId, isSuperAdmin);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"autogallery-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options, tenantService, currentUserService, new FakeDateTime());
    }
}

public sealed class FakeTenantService : ITenantService
{
    private Guid? _tenantId;

    public FakeTenantService(Guid? tenantId) => _tenantId = tenantId;

    public Guid? GetTenantId() => _tenantId;

    public void SetTenantId(Guid tenantId) => _tenantId = tenantId;
}

public sealed class FakeCurrentUserService : ICurrentUserService
{
    public FakeCurrentUserService(Guid? tenantId, bool isSuperAdmin)
    {
        TenantId = tenantId;
        IsSuperAdmin = isSuperAdmin;
        UserId = Guid.NewGuid().ToString();
    }

    public string? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public bool IsAuthenticated => true;
    public bool IsSuperAdmin { get; set; }
}

public sealed class FakeDateTime : IDateTime
{
    public DateTime Now { get; set; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
