using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Domain.Common;
using AutoGallerySaaS.Domain.Entities.Crm;
using AutoGallerySaaS.Domain.Entities.Finance;
using AutoGallerySaaS.Domain.Entities.Identity;
using AutoGallerySaaS.Domain.Entities.SaaS;
using AutoGallerySaaS.Domain.Entities.Vehicles;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

using AutoGallerySaaS.Application.Common.Interfaces;

namespace AutoGallerySaaS.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        IDateTime dateTime) : base(options)
    {
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<TenantSetting> TenantSettings => Set<TenantSetting>();

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleExpense> VehicleExpenses => Set<VehicleExpense>();
    public DbSet<VehicleAttachment> VehicleAttachments => Set<VehicleAttachment>();

    public DbSet<IncomeCategory> IncomeCategories => Set<IncomeCategory>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global Query Filters
        var tenantId = _tenantService.GetTenantId();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
                var filter = Expression.Lambda(Expression.Equal(property, Expression.Constant(tenantId ?? Guid.Empty)), parameter);

                // This is simplified. Real world would handle null tenantId for superadmins etc.
                // But per requirements: "Backend sorguları otomatik tenant filtresi ile çalışmalı."
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }

            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                var filter = Expression.Lambda(Expression.Equal(property, Expression.Constant(false)), parameter);

                // Combining filters is complex with Expressions, so we usually use a library or more robust approach.
                // For this task, I'll stick to basic implementation or focus on TenantId as most critical.
                // EF Core only supports one query filter per entity. So we need to combine them.
            }
        }

        // Better way to handle combined filters:
        ConfigureGlobalFilters(modelBuilder);

        // Identity Configuration
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Tenant>().HasIndex(t => t.Identifier).IsUnique();
    }

    private void ConfigureGlobalFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var isTenantEntity = typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType);
            var isSoftDelete = typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType);

            if (isTenantEntity || isSoftDelete)
            {
                var method = typeof(ApplicationDbContext).GetMethod(nameof(GetFilterExpression), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType.ClrType);

                if (method != null)
                {
                    var filter = method.Invoke(this, null);
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter((LambdaExpression)filter!);
                }
            }
        }
    }

    private Expression<Func<TEntity, bool>> GetFilterExpression<TEntity>() where TEntity : class
    {
        Expression<Func<TEntity, bool>> expression = e => true;

        if (typeof(ITenantEntity).IsAssignableFrom(typeof(TEntity)))
        {
            var tenantId = _tenantService.GetTenantId();
            Expression<Func<TEntity, bool>> tenantFilter = e => ((ITenantEntity)e).TenantId == (tenantId ?? Guid.Empty);
            expression = CombineExpressions(expression, tenantFilter);
        }

        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            Expression<Func<TEntity, bool>> softDeleteFilter = e => !((ISoftDelete)e).IsDeleted;
            expression = CombineExpressions(expression, softDeleteFilter);
        }

        return expression;
    }

    private Expression<Func<T, bool>> CombineExpressions<T>(Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
    {
        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
        var left = leftVisitor.Visit(expr1.Body);

        var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
        var right = rightVisitor.Visit(expr2.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left, right), parameter);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = _dateTime.Now;
                    entry.Entity.CreatedBy = _currentUserService.UserId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = _dateTime.Now;
                    entry.Entity.UpdatedBy = _currentUserService.UserId;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.TenantId == Guid.Empty)
                {
                    entry.Entity.TenantId = _tenantService.GetTenantId() ?? Guid.Empty;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

public class ReplaceExpressionVisitor : ExpressionVisitor
{
    private readonly Expression _oldValue;
    private readonly Expression _newValue;

    public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
    {
        _oldValue = oldValue;
        _newValue = newValue;
    }

    public override Expression Visit(Expression? node)
    {
        if (node == _oldValue) return _newValue;
        return base.Visit(node)!;
    }
}
