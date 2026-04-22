using AutoGallerySaaS.Domain.Entities.Crm;
using AutoGallerySaaS.Domain.Entities.Finance;
using AutoGallerySaaS.Domain.Entities.Identity;
using AutoGallerySaaS.Domain.Entities.SaaS;
using AutoGallerySaaS.Domain.Entities.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<TenantSetting> TenantSettings { get; }

    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }

    DbSet<Vehicle> Vehicles { get; }
    DbSet<VehicleExpense> VehicleExpenses { get; }
    DbSet<VehicleAttachment> VehicleAttachments { get; }

    DbSet<IncomeCategory> IncomeCategories { get; }
    DbSet<ExpenseCategory> ExpenseCategories { get; }
    DbSet<Transaction> Transactions { get; }

    DbSet<Customer> Customers { get; }
    DbSet<Supplier> Suppliers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
