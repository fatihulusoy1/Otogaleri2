using AutoGallerySaaS.Domain.Entities.Identity;
using AutoGallerySaaS.Domain.Entities.SaaS;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (!await context.SubscriptionPlans.IgnoreQueryFilters().AnyAsync())
        {
            context.SubscriptionPlans.AddRange(
                new SubscriptionPlan { Name = "Free", Description = "Demo Plan", MonthlyPrice = 0, YearlyPrice = 0, MaxUsers = 2, MaxVehicles = 5 },
                new SubscriptionPlan { Name = "Basic", Description = "Basic Plan", MonthlyPrice = 99, YearlyPrice = 990, MaxUsers = 5, MaxVehicles = 50 },
                new SubscriptionPlan { Name = "Pro", Description = "Pro Plan", MonthlyPrice = 299, YearlyPrice = 2990, MaxUsers = 20, MaxVehicles = 500 });
            await context.SaveChangesAsync();
        }

        var defaultPermissions = new List<Permission>
        {
            new() { Name = "Vehicle.View", Code = "Vehicles.View", Group = "Vehicles" },
            new() { Name = "Vehicle.Create", Code = "Vehicles.Create", Group = "Vehicles" },
            new() { Name = "Vehicle.Edit", Code = "Vehicles.Edit", Group = "Vehicles" },
            new() { Name = "Vehicle.Delete", Code = "Vehicles.Delete", Group = "Vehicles" },
            new() { Name = "Dashboard.View", Code = "Dashboard.View", Group = "Dashboard" },
            new() { Name = "Finance.Manage", Code = "Finance.Manage", Group = "Finance" }
        };

        var existingPermissionCodes = await context.Permissions
            .IgnoreQueryFilters()
            .Select(permission => permission.Code)
            .ToListAsync();

        context.Permissions.AddRange(defaultPermissions.Where(permission => !existingPermissionCodes.Contains(permission.Code)));
        await context.SaveChangesAsync();

        var freePlan = await context.SubscriptionPlans.IgnoreQueryFilters().FirstAsync(plan => plan.Name == "Free");
        var demoTenant = await context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(tenant => tenant.Identifier == "demo");

        if (demoTenant == null)
        {
            demoTenant = new Tenant
            {
                Name = "Demo Gallery",
                Identifier = "demo",
                IsActive = true,
                SubscriptionPlanId = freePlan.Id,
                SubscriptionEndDate = DateTime.UtcNow.AddYears(1)
            };
            context.Tenants.Add(demoTenant);
            await context.SaveChangesAsync();
        }

        var superAdmin = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(user => user.Email == "admin@autogallery.com");

        if (superAdmin == null)
        {
            superAdmin = new User
            {
                FirstName = "Super",
                LastName = "Admin",
                Email = "admin@autogallery.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                IsSuperAdmin = true,
                TenantId = demoTenant.Id,
                IsActive = true
            };
            context.Users.Add(superAdmin);
        }

        var tenantAdmin = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(user => user.Email == "demo@autogallery.com");

        if (tenantAdmin == null)
        {
            tenantAdmin = new User
            {
                FirstName = "Demo",
                LastName = "Admin",
                Email = "demo@autogallery.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("DemoAdmin123!"),
                TenantId = demoTenant.Id,
                IsActive = true
            };
            context.Users.Add(tenantAdmin);
        }

        await context.SaveChangesAsync();

        var superAdminRole = await GetOrCreateRoleAsync(context, demoTenant.Id, "SuperAdmin", "System super administrator");
        var tenantAdminRole = await GetOrCreateRoleAsync(context, demoTenant.Id, "TenantAdmin", "Tenant administrator");

        await AddUserRoleIfMissingAsync(context, demoTenant.Id, superAdmin.Id, superAdminRole.Id);
        await AddUserRoleIfMissingAsync(context, demoTenant.Id, tenantAdmin.Id, tenantAdminRole.Id);

        var permissionIds = await context.Permissions.IgnoreQueryFilters().Select(permission => permission.Id).ToListAsync();
        foreach (var permissionId in permissionIds)
        {
            var rolePermissionExists = await context.RolePermissions
                .IgnoreQueryFilters()
                .AnyAsync(rolePermission => rolePermission.RoleId == tenantAdminRole.Id && rolePermission.PermissionId == permissionId);

            if (!rolePermissionExists)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    TenantId = demoTenant.Id,
                    RoleId = tenantAdminRole.Id,
                    PermissionId = permissionId
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task<Role> GetOrCreateRoleAsync(
        ApplicationDbContext context,
        Guid tenantId,
        string roleName,
        string description)
    {
        var role = await context.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(existingRole => existingRole.TenantId == tenantId && existingRole.Name == roleName);

        if (role != null)
        {
            return role;
        }

        role = new Role
        {
            TenantId = tenantId,
            Name = roleName,
            Description = description,
            IsStatic = true
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync();
        return role;
    }

    private static async Task AddUserRoleIfMissingAsync(ApplicationDbContext context, Guid tenantId, Guid userId, Guid roleId)
    {
        var relationExists = await context.UserRoles
            .IgnoreQueryFilters()
            .AnyAsync(userRole => userRole.UserId == userId && userRole.RoleId == roleId);

        if (!relationExists)
        {
            context.UserRoles.Add(new UserRole
            {
                TenantId = tenantId,
                UserId = userId,
                RoleId = roleId
            });
            await context.SaveChangesAsync();
        }
    }
}
