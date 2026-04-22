using AutoGallerySaaS.Domain.Entities.Identity;
using AutoGallerySaaS.Domain.Entities.SaaS;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (!context.SubscriptionPlans.Any())
        {
            var plans = new List<SubscriptionPlan>
            {
                new() { Name = "Free", Description = "Demo Plan", MonthlyPrice = 0, YearlyPrice = 0, MaxUsers = 2, MaxVehicles = 5 },
                new() { Name = "Basic", Description = "Basic Plan", MonthlyPrice = 99, YearlyPrice = 990, MaxUsers = 5, MaxVehicles = 50 },
                new() { Name = "Pro", Description = "Pro Plan", MonthlyPrice = 299, YearlyPrice = 2990, MaxUsers = 20, MaxVehicles = 500 }
            };
            context.SubscriptionPlans.AddRange(plans);
            await context.SaveChangesAsync();
        }

        if (!context.Tenants.Any())
        {
            var demoPlan = await context.SubscriptionPlans.FirstAsync();
            var demoTenant = new Tenant
            {
                Name = "Demo Gallery",
                Identifier = "demo",
                IsActive = true,
                SubscriptionPlanId = demoPlan.Id,
                SubscriptionEndDate = DateTime.UtcNow.AddYears(1)
            };
            context.Tenants.Add(demoTenant);
            await context.SaveChangesAsync();

            // Seed Permissions
            var permissions = new List<Permission>
            {
                new() { Name = "Vehicle.View", Code = "Vehicles.View", Group = "Vehicles" },
                new() { Name = "Vehicle.Create", Code = "Vehicles.Create", Group = "Vehicles" },
                new() { Name = "Vehicle.Edit", Code = "Vehicles.Edit", Group = "Vehicles" },
                new() { Name = "Vehicle.Delete", Code = "Vehicles.Delete", Group = "Vehicles" }
            };
            context.Permissions.AddRange(permissions);
            await context.SaveChangesAsync();

            // Seed Super Admin
            var superAdmin = new User
            {
                FirstName = "Super",
                LastName = "Admin",
                Email = "admin@autogallery.com",
                PasswordHash = "AQAAAAIAAYagAAAAE...", // Dummy hash
                IsSuperAdmin = true,
                TenantId = demoTenant.Id
            };
            context.Users.Add(superAdmin);
            await context.SaveChangesAsync();
        }
    }
}
