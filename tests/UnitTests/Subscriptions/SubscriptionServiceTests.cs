using AutoGallerySaaS.Application.Common.Exceptions;
using AutoGallerySaaS.Application.Features.Subscriptions.Services;
using AutoGallerySaaS.Domain.Entities.Identity;
using AutoGallerySaaS.Domain.Entities.SaaS;
using AutoGallerySaaS.Domain.Entities.Vehicles;
using AutoGallerySaaS.UnitTests.TestSupport;
using FluentAssertions;

namespace AutoGallerySaaS.UnitTests.Subscriptions;

public class SubscriptionServiceTests
{
    private static SubscriptionPlan NewPlan(int maxVehicles = 5, int maxUsers = 5)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = "Test Plan",
            Description = "Test",
            MonthlyPrice = 0,
            YearlyPrice = 0,
            MaxVehicles = maxVehicles,
            MaxUsers = maxUsers
        };

    private static Tenant NewTenant(Guid tenantId, Guid planId)
        => new()
        {
            Id = tenantId,
            Name = "Test Tenant",
            SubscriptionPlanId = planId,
            SubscriptionEndDate = DateTime.UtcNow.AddDays(30),
            IsActive = true
        };

    private static Vehicle NewVehicle(Guid tenantId)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Plate = "34TEST" + Guid.NewGuid().ToString("N")[..4],
            Brand = "BMW",
            Model = "320i",
            Color = "Siyah",
            EngineNumber = "ENG",
            ChassisNumber = "CHS",
            Year = 2024,
            PurchasePrice = 100000m,
            Status = VehicleStatus.InStock
        };

    [Fact]
    public async Task EnsureCanAddVehicleAsync_WhenAtLimit_ThrowsSubscriptionException()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId);
        var plan = NewPlan(maxVehicles: 2);
        context.SubscriptionPlans.Add(plan);
        context.Tenants.Add(NewTenant(tenantId, plan.Id));
        context.Vehicles.AddRange(NewVehicle(tenantId), NewVehicle(tenantId));
        await context.SaveChangesAsync();

        var sut = new SubscriptionService(context);

        var act = () => sut.EnsureCanAddVehicleAsync();

        await act.Should().ThrowAsync<SubscriptionException>();
    }

    [Fact]
    public async Task EnsureCanAddVehicleAsync_WhenUnderLimit_DoesNotThrow()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId);
        var plan = NewPlan(maxVehicles: 5);
        context.SubscriptionPlans.Add(plan);
        context.Tenants.Add(NewTenant(tenantId, plan.Id));
        context.Vehicles.Add(NewVehicle(tenantId));
        await context.SaveChangesAsync();

        var sut = new SubscriptionService(context);

        var act = () => sut.EnsureCanAddVehicleAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureCanAddVehicleAsync_AsSuperAdmin_DoesNotThrowEvenOverLimit()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId, isSuperAdmin: true);
        var plan = NewPlan(maxVehicles: 1);
        context.SubscriptionPlans.Add(plan);
        context.Tenants.Add(NewTenant(tenantId, plan.Id));
        context.Vehicles.AddRange(NewVehicle(tenantId), NewVehicle(tenantId), NewVehicle(tenantId));
        await context.SaveChangesAsync();

        var sut = new SubscriptionService(context);

        var act = () => sut.EnsureCanAddVehicleAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureCanAddUserAsync_WhenAtLimit_ThrowsSubscriptionException()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId);
        var plan = NewPlan(maxUsers: 1);
        context.SubscriptionPlans.Add(plan);
        context.Tenants.Add(NewTenant(tenantId, plan.Id));
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = "Mevcut",
            LastName = "Kullanici",
            Email = "mevcut@test.com",
            PasswordHash = "hash",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var sut = new SubscriptionService(context);

        var act = () => sut.EnsureCanAddUserAsync();

        await act.Should().ThrowAsync<SubscriptionException>();
    }
}
