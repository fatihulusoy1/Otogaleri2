using AutoGallerySaaS.Application.Common.Exceptions;
using AutoGallerySaaS.Application.Features.Auth.Dtos;
using AutoGallerySaaS.Application.Features.Auth.Services;
using AutoGallerySaaS.Domain.Entities.Identity;
using AutoGallerySaaS.Domain.Entities.SaaS;
using AutoGallerySaaS.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.UnitTests.Auth;

public class AuthServiceTests
{
    private const string Password = "Password123!";

    private static SubscriptionPlan NewPlan(string name)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = name,
            MonthlyPrice = 0,
            YearlyPrice = 0,
            MaxVehicles = 500,
            MaxUsers = 20
        };

    private static User NewUser(Guid tenantId, bool isSuperAdmin = false)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = "Test",
            LastName = "User",
            Email = "user@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
            IsActive = true,
            IsSuperAdmin = isSuperAdmin
        };

    [Fact]
    public async Task LoginAsync_WhenSubscriptionExpired_ThrowsSubscriptionException()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId);
        var plan = NewPlan("Pro");
        context.SubscriptionPlans.Add(plan);
        context.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            SubscriptionPlanId = plan.Id,
            SubscriptionEndDate = DateTime.UtcNow.AddDays(-1),
            IsTrial = true,
            IsActive = true
        });
        context.Users.Add(NewUser(tenantId));
        await context.SaveChangesAsync();

        var sut = new AuthService(context, new FakeJwtService());

        var act = () => sut.LoginAsync(new LoginRequest("user@test.com", Password));

        await act.Should().ThrowAsync<SubscriptionException>();
    }

    [Fact]
    public async Task LoginAsync_WhenSuperAdminAndSubscriptionExpired_Succeeds()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId);
        var plan = NewPlan("Pro");
        context.SubscriptionPlans.Add(plan);
        context.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            SubscriptionPlanId = plan.Id,
            SubscriptionEndDate = DateTime.UtcNow.AddDays(-1),
            IsTrial = false,
            IsActive = true
        });
        context.Users.Add(NewUser(tenantId, isSuperAdmin: true));
        await context.SaveChangesAsync();

        var sut = new AuthService(context, new FakeJwtService());

        var result = await sut.LoginAsync(new LoginRequest("user@test.com", Password));

        result.Token.Should().Be("test-token");
        result.IsSuperAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_StartsFourteenDayTrialOnProPlan()
    {
        var tenantId = Guid.NewGuid();
        using var context = TestContext.Create(out _, out _, tenantId);
        var proPlan = NewPlan("Pro");
        context.SubscriptionPlans.AddRange(NewPlan("Free"), proPlan);
        await context.SaveChangesAsync();

        var sut = new AuthService(context, new FakeJwtService());

        var result = await sut.RegisterAsync(new RegisterRequest(
            "owner@galeri.com",
            Password,
            "Galeri",
            "Sahibi",
            "Ornek Oto"));

        result.Token.Should().Be("test-token");

        var createdTenant = await context.Tenants
            .IgnoreQueryFilters()
            .FirstAsync(tenant => tenant.Name == "Ornek Oto");

        createdTenant.IsTrial.Should().BeTrue();
        createdTenant.SubscriptionPlanId.Should().Be(proPlan.Id);
        createdTenant.SubscriptionEndDate.Should().BeAfter(DateTime.UtcNow.AddDays(13));
        createdTenant.SubscriptionEndDate.Should().BeBefore(DateTime.UtcNow.AddDays(15));
    }
}
