using AutoGallerySaaS.Application.Common.Exceptions;
using AutoGallerySaaS.Application.Features.Admin.Dtos;
using AutoGallerySaaS.Application.Features.Admin.Services;
using AutoGallerySaaS.Domain.Entities.Identity;
using AutoGallerySaaS.Domain.Entities.SaaS;
using AutoGallerySaaS.Domain.Entities.Vehicles;
using AutoGallerySaaS.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.UnitTests.Admin;

public class AdminServiceTenantTests
{
    private static SubscriptionPlan NewPlan(string name, int maxVehicles = 50, int maxUsers = 5)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = name + " plan",
            MonthlyPrice = 99,
            YearlyPrice = 990,
            MaxVehicles = maxVehicles,
            MaxUsers = maxUsers,
            IsActive = true
        };

    private static Tenant NewTenant(Guid id, Guid planId, bool isTrial = true)
        => new()
        {
            Id = id,
            Name = "Galeri",
            SubscriptionPlanId = planId,
            SubscriptionEndDate = DateTime.UtcNow.AddDays(10),
            IsTrial = isTrial,
            IsActive = true
        };

    private static Vehicle NewVehicle(Guid tenantId)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Plate = "34X" + Guid.NewGuid().ToString("N")[..5],
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
    public async Task GetTenantsAsync_ReturnsEnrichedPlanAndCounts()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId, isSuperAdmin: true);
        var plan = NewPlan("Pro");
        context.SubscriptionPlans.Add(plan);
        context.Tenants.Add(NewTenant(tenantId, plan.Id));
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = "A",
            LastName = "B",
            Email = "a@b.com",
            PasswordHash = "hash",
            IsActive = true
        });
        context.Vehicles.AddRange(NewVehicle(tenantId), NewVehicle(tenantId));
        await context.SaveChangesAsync();

        var sut = new AdminService(context);

        var tenants = await sut.GetTenantsAsync();

        var dto = tenants.Should().ContainSingle().Subject;
        dto.SubscriptionPlanName.Should().Be("Pro");
        dto.UserCount.Should().Be(1);
        dto.VehicleCount.Should().Be(2);
        dto.IsTrial.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTenantSubscriptionAsync_ChangesPlanEndDateAndTrial()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId, isSuperAdmin: true);
        var basic = NewPlan("Basic");
        var pro = NewPlan("Pro");
        context.SubscriptionPlans.AddRange(basic, pro);
        context.Tenants.Add(NewTenant(tenantId, basic.Id, isTrial: true));
        await context.SaveChangesAsync();

        var sut = new AdminService(context);
        var newEnd = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(365), DateTimeKind.Utc);

        await sut.UpdateTenantSubscriptionAsync(tenantId, new UpdateTenantSubscriptionRequest(pro.Id, newEnd, false));

        var tenant = await context.Tenants.IgnoreQueryFilters().FirstAsync(item => item.Id == tenantId);
        tenant.SubscriptionPlanId.Should().Be(pro.Id);
        tenant.IsTrial.Should().BeFalse();
        tenant.SubscriptionEndDate.Should().BeCloseTo(newEnd, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateTenantSubscriptionAsync_UnknownPlan_ThrowsNotFound()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId, isSuperAdmin: true);
        var plan = NewPlan("Basic");
        context.SubscriptionPlans.Add(plan);
        context.Tenants.Add(NewTenant(tenantId, plan.Id));
        await context.SaveChangesAsync();

        var sut = new AdminService(context);

        var act = () => sut.UpdateTenantSubscriptionAsync(
            tenantId,
            new UpdateTenantSubscriptionRequest(Guid.NewGuid(), DateTime.UtcNow.AddDays(30), false));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteTenantAsync_SoftDeletesTenant()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId, isSuperAdmin: true);
        var plan = NewPlan("Basic");
        context.SubscriptionPlans.Add(plan);
        context.Tenants.Add(NewTenant(tenantId, plan.Id));
        await context.SaveChangesAsync();

        var sut = new AdminService(context);

        await sut.DeleteTenantAsync(tenantId);

        var tenant = await context.Tenants.IgnoreQueryFilters().FirstAsync(item => item.Id == tenantId);
        tenant.IsDeleted.Should().BeTrue();
        tenant.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTenantAsync_WithSuperAdmin_ThrowsBusinessRule()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId, isSuperAdmin: true);
        var plan = NewPlan("Basic");
        context.SubscriptionPlans.Add(plan);
        context.Tenants.Add(NewTenant(tenantId, plan.Id));
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = "Super",
            LastName = "Admin",
            Email = "super@b.com",
            PasswordHash = "hash",
            IsActive = true,
            IsSuperAdmin = true
        });
        await context.SaveChangesAsync();

        var sut = new AdminService(context);

        var act = () => sut.DeleteTenantAsync(tenantId);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreatePlanAsync_DuplicateName_ThrowsBusinessRule()
    {
        using var context = TestContext.Create(out _, out _, Guid.NewGuid(), isSuperAdmin: true);
        context.SubscriptionPlans.Add(NewPlan("Pro"));
        await context.SaveChangesAsync();

        var sut = new AdminService(context);

        var act = () => sut.CreatePlanAsync(new SaveSubscriptionPlanRequest("Pro", "x", 1, 10, 5, 50, true));

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreatePlanAsync_InvalidLimit_ThrowsValidation()
    {
        using var context = TestContext.Create(out _, out _, Guid.NewGuid(), isSuperAdmin: true);
        var sut = new AdminService(context);

        var act = () => sut.CreatePlanAsync(new SaveSubscriptionPlanRequest("Enterprise", "x", 1, 10, 0, 50, true));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdatePlanAsync_UpdatesFields()
    {
        using var context = TestContext.Create(out _, out _, Guid.NewGuid(), isSuperAdmin: true);
        var plan = NewPlan("Basic");
        context.SubscriptionPlans.Add(plan);
        await context.SaveChangesAsync();

        var sut = new AdminService(context);

        var result = await sut.UpdatePlanAsync(plan.Id, new SaveSubscriptionPlanRequest("Basic", "Yeni aciklama", 149, 1490, 8, 80, false));

        result.MonthlyPrice.Should().Be(149);
        result.MaxVehicles.Should().Be(80);
        result.IsActive.Should().BeFalse();
    }
}
