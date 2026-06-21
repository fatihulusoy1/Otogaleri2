using System.Linq.Expressions;
using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Domain.Common;
using AutoGallerySaaS.Domain.Entities.Crm;
using AutoGallerySaaS.Domain.Entities.Finance;
using AutoGallerySaaS.Domain.Entities.Identity;
using AutoGallerySaaS.Domain.Entities.SaaS;
using AutoGallerySaaS.Domain.Entities.Vehicles;
using Microsoft.EntityFrameworkCore;

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
    public DbSet<VehicleSegment> VehicleSegments => Set<VehicleSegment>();
    public DbSet<VehicleBrand> VehicleBrands => Set<VehicleBrand>();
    public DbSet<VehicleCatalogModel> VehicleCatalogModels => Set<VehicleCatalogModel>();
    public DbSet<Consignment> Consignments => Set<Consignment>();
    public DbSet<VehicleTrade> VehicleTrades => Set<VehicleTrade>();
    public DbSet<VehicleExpense> VehicleExpenses => Set<VehicleExpense>();
    public DbSet<VehicleAttachment> VehicleAttachments => Set<VehicleAttachment>();

    public DbSet<IncomeCategory> IncomeCategories => Set<IncomeCategory>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<VehicleExpenseCategory> VehicleExpenseCategories => Set<VehicleExpenseCategory>();
    public DbSet<GeneralExpenseCategory> GeneralExpenseCategories => Set<GeneralExpenseCategory>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<ReceivablePayable> ReceivablePayables => Set<ReceivablePayable>();

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public Guid? CurrentTenantId => _tenantService.GetTenantId();
    public bool CurrentUserIsSuperAdmin => _currentUserService.IsSuperAdmin;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureGlobalFilters(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Tenant>().HasIndex(t => t.Identifier).IsUnique();
        modelBuilder.Entity<VehicleSegment>().HasIndex(segment => new { segment.TenantId, segment.Name }).IsUnique();
        modelBuilder.Entity<VehicleBrand>().HasIndex(brand => new { brand.TenantId, brand.Name }).IsUnique();
        modelBuilder.Entity<VehicleExpenseCategory>().HasIndex(category => new { category.TenantId, category.Name }).IsUnique();
        modelBuilder.Entity<GeneralExpenseCategory>().HasIndex(category => new { category.TenantId, category.Name }).IsUnique();
        modelBuilder.Entity<ReceivablePayable>()
            .HasIndex(item => new { item.TenantId, item.Type, item.Status, item.DueDate });
        modelBuilder.Entity<VehicleCatalogModel>()
            .HasIndex(model => new { model.TenantId, model.VehicleBrandId, model.VehicleSegmentId, model.Name })
            .IsUnique();
        modelBuilder.Entity<Consignment>()
            .HasIndex(consignment => new { consignment.TenantId, consignment.Type, consignment.Status });
        modelBuilder.Entity<VehicleTrade>()
            .HasIndex(trade => new { trade.TenantId, trade.OperationType, trade.SourceVehicleId });

        modelBuilder.Entity<Vehicle>()
            .HasOne(vehicle => vehicle.SegmentLookup)
            .WithMany(segment => segment.Vehicles)
            .HasForeignKey(vehicle => vehicle.SegmentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Vehicle>()
            .HasOne(vehicle => vehicle.BrandLookup)
            .WithMany(brand => brand.Vehicles)
            .HasForeignKey(vehicle => vehicle.BrandId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Vehicle>()
            .HasOne(vehicle => vehicle.ModelLookup)
            .WithMany(model => model.Vehicles)
            .HasForeignKey(vehicle => vehicle.ModelId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<VehicleCatalogModel>()
            .HasOne(model => model.VehicleBrand)
            .WithMany(brand => brand.Models)
            .HasForeignKey(model => model.VehicleBrandId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VehicleCatalogModel>()
            .HasOne(model => model.VehicleSegment)
            .WithMany(segment => segment.Models)
            .HasForeignKey(model => model.VehicleSegmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private void ConfigureGlobalFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var isTenantEntity = typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType);
            var isSoftDelete = typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType);

            if (!isTenantEntity && !isSoftDelete)
            {
                continue;
            }

            var method = typeof(ApplicationDbContext)
                .GetMethod(nameof(GetFilterExpression), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .MakeGenericMethod(entityType.ClrType);

            if (method == null)
            {
                continue;
            }

            var filter = method.Invoke(this, null);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter((LambdaExpression)filter!);
        }
    }

    private Expression<Func<TEntity, bool>> GetFilterExpression<TEntity>() where TEntity : class
    {
        Expression<Func<TEntity, bool>> expression = _ => true;

        if (typeof(ITenantEntity).IsAssignableFrom(typeof(TEntity)))
        {
            Expression<Func<TEntity, bool>> tenantFilter =
                entity => CurrentUserIsSuperAdmin || ((ITenantEntity)entity).TenantId == (CurrentTenantId ?? Guid.Empty);
            expression = CombineExpressions(expression, tenantFilter);
        }

        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            Expression<Func<TEntity, bool>> softDeleteFilter = entity => !((ISoftDelete)entity).IsDeleted;
            expression = CombineExpressions(expression, softDeleteFilter);
        }

        return expression;
    }

    private static Expression<Func<T, bool>> CombineExpressions<T>(
        Expression<Func<T, bool>> leftExpression,
        Expression<Func<T, bool>> rightExpression)
    {
        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ReplaceExpressionVisitor(leftExpression.Parameters[0], parameter);
        var left = leftVisitor.Visit(leftExpression.Body);

        var rightVisitor = new ReplaceExpressionVisitor(rightExpression.Parameters[0], parameter);
        var right = rightVisitor.Visit(rightExpression.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left!, right!), parameter);
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
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                entry.Entity.TenantId = _tenantService.GetTenantId() ?? Guid.Empty;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State == EntityState.Modified && entry.Entity.IsDeleted && entry.Entity.DeletedAt == null)
            {
                entry.Entity.DeletedAt = _dateTime.Now;
                entry.Entity.DeletedBy = _currentUserService.UserId;
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
        if (node == _oldValue)
        {
            return _newValue;
        }

        return base.Visit(node)!;
    }
}
