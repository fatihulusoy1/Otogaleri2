using AutoGallerySaaS.Application.Common;
using AutoGallerySaaS.Domain.Entities.Finance;
using AutoGallerySaaS.Domain.Entities.Identity;
using AutoGallerySaaS.Domain.Entities.SaaS;
using AutoGallerySaaS.Domain.Entities.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Persistence;

public static class DbInitializer
{
    private static readonly Guid SharedLookupTenantId = SharedTenantIds.Catalog;

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await EnsureVehicleCatalogSchemaAsync(context);
        await NormalizeSharedLookupDataAsync(context);

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

        await SeedVehicleLookupsAsync(context);
        await SeedExpenseCategoriesAsync(context);

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

    private static async Task EnsureVehicleCatalogSchemaAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "Vehicles" ADD COLUMN IF NOT EXISTS "SegmentId" uuid NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "Vehicles" ADD COLUMN IF NOT EXISTS "Segment" text NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "Vehicles" ADD COLUMN IF NOT EXISTS "BrandId" uuid NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "Vehicles" ADD COLUMN IF NOT EXISTS "ModelId" uuid NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "Vehicles" ADD COLUMN IF NOT EXISTS "PurchaseDate" timestamp with time zone NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "Vehicles" ADD COLUMN IF NOT EXISTS "PurchasePaymentMethod" integer NOT NULL DEFAULT 1;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "Vehicles" ADD COLUMN IF NOT EXISTS "PurchaseNotaryRegistryNumber" text NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "Vehicles" ADD COLUMN IF NOT EXISTS "PurchaseTradePlate" text NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "Vehicles" ADD COLUMN IF NOT EXISTS "PurchaseTradeAmount" numeric NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "Vehicles" ADD COLUMN IF NOT EXISTS "SaleDate" timestamp with time zone NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "Vehicles" ADD COLUMN IF NOT EXISTS "SaleTradePlate" text NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "Vehicles" ADD COLUMN IF NOT EXISTS "SaleTradeAmount" numeric NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "Vehicles" ADD COLUMN IF NOT EXISTS "OwnershipType" integer NOT NULL DEFAULT 1;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "Vehicles" ADD COLUMN IF NOT EXISTS "ConsignmentId" uuid NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            UPDATE "Vehicles"
            SET "PurchaseDate" = COALESCE("PurchaseDate", "CreatedAt")
            WHERE "PurchaseDate" IS NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Consignments" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "Type" integer NOT NULL,
                "Status" integer NOT NULL,
                "VehicleId" uuid NULL,
                "SegmentId" uuid NULL,
                "Segment" text NULL,
                "BrandId" uuid NULL,
                "Brand" text NOT NULL,
                "ModelId" uuid NULL,
                "Model" text NOT NULL,
                "Plate" text NOT NULL,
                "Year" integer NOT NULL,
                "Color" text NOT NULL,
                "EngineNumber" text NOT NULL,
                "ChassisNumber" text NOT NULL,
                "OwnerName" text NOT NULL,
                "OwnerPhone" text NULL,
                "CustomerName" text NULL,
                "CustomerPhone" text NULL,
                "ConsignmentDate" timestamp with time zone NOT NULL,
                "PurchasePrice" numeric NULL,
                "ExpectedSalePrice" numeric NULL,
                "CommissionAmount" numeric NULL,
                "CommissionRate" numeric NULL,
                "SalePrice" numeric NULL,
                "NetAmountToOwner" numeric NULL,
                "SaleDate" timestamp with time zone NULL,
                "Description" text NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "CreatedBy" text NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedBy" text NULL,
                "DeletedAt" timestamp with time zone NULL,
                "DeletedBy" text NULL,
                "IsDeleted" boolean NOT NULL DEFAULT FALSE
            );
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_Consignments_TenantId_Type_Status" ON "Consignments" ("TenantId", "Type", "Status");
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "VehicleTrades" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "OperationType" integer NOT NULL,
                "SourceVehicleId" uuid NOT NULL,
                "TradeVehicleId" uuid NOT NULL,
                "AgreedValue" numeric NOT NULL,
                "CashAmount" numeric NOT NULL,
                "Description" text NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "CreatedBy" text NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedBy" text NULL,
                "DeletedAt" timestamp with time zone NULL,
                "DeletedBy" text NULL,
                "IsDeleted" boolean NOT NULL DEFAULT FALSE
            );
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_VehicleTrades_TenantId_OperationType_SourceVehicleId"
            ON "VehicleTrades" ("TenantId", "OperationType", "SourceVehicleId");
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "VehicleSegments" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "Name" text NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "CreatedBy" text NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedBy" text NULL,
                "DeletedAt" timestamp with time zone NULL,
                "DeletedBy" text NULL,
                "IsDeleted" boolean NOT NULL DEFAULT FALSE
            );
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "VehicleBrands" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "Name" text NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "CreatedBy" text NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedBy" text NULL,
                "DeletedAt" timestamp with time zone NULL,
                "DeletedBy" text NULL,
                "IsDeleted" boolean NOT NULL DEFAULT FALSE
            );
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "VehicleCatalogModels" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "Name" text NOT NULL,
                "VehicleBrandId" uuid NOT NULL,
                "VehicleSegmentId" uuid NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "CreatedBy" text NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedBy" text NULL,
                "DeletedAt" timestamp with time zone NULL,
                "DeletedBy" text NULL,
                "IsDeleted" boolean NOT NULL DEFAULT FALSE
            );
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "VehicleSegments" ADD COLUMN IF NOT EXISTS "DeletedAt" timestamp with time zone NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "VehicleSegments" ADD COLUMN IF NOT EXISTS "DeletedBy" text NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "VehicleBrands" ADD COLUMN IF NOT EXISTS "DeletedAt" timestamp with time zone NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "VehicleBrands" ADD COLUMN IF NOT EXISTS "DeletedBy" text NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "VehicleCatalogModels" ADD COLUMN IF NOT EXISTS "DeletedAt" timestamp with time zone NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "VehicleCatalogModels" ADD COLUMN IF NOT EXISTS "DeletedBy" text NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_VehicleSegments_TenantId_Name" ON "VehicleSegments" ("TenantId", "Name");
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_VehicleBrands_TenantId_Name" ON "VehicleBrands" ("TenantId", "Name");
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_VehicleCatalogModels_TenantId_VehicleBrandId_VehicleSegmentId_Name"
            ON "VehicleCatalogModels" ("TenantId", "VehicleBrandId", "VehicleSegmentId", "Name");
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "ExpenseCategories" ADD COLUMN IF NOT EXISTS "CategoryType" integer NOT NULL DEFAULT 1;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "VehicleExpenseCategories" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "Name" text NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "CreatedBy" text NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedBy" text NULL,
                "DeletedAt" timestamp with time zone NULL,
                "DeletedBy" text NULL,
                "IsDeleted" boolean NOT NULL DEFAULT FALSE
            );
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "GeneralExpenseCategories" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "Name" text NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "CreatedBy" text NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedBy" text NULL,
                "DeletedAt" timestamp with time zone NULL,
                "DeletedBy" text NULL,
                "IsDeleted" boolean NOT NULL DEFAULT FALSE
            );
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_VehicleExpenseCategories_TenantId_Name" ON "VehicleExpenseCategories" ("TenantId", "Name");
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_GeneralExpenseCategories_TenantId_Name" ON "GeneralExpenseCategories" ("TenantId", "Name");
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ReceivablePayables" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "Type" integer NOT NULL,
                "SourceType" integer NOT NULL,
                "SourceId" uuid NOT NULL,
                "CounterpartyName" text NOT NULL,
                "PaymentMethod" integer NOT NULL,
                "DocumentType" integer NOT NULL,
                "DocumentNumber" text NULL,
                "IssueDate" timestamp with time zone NOT NULL,
                "DueDate" timestamp with time zone NOT NULL,
                "OriginalAmount" numeric NOT NULL,
                "RemainingAmount" numeric NOT NULL,
                "Status" integer NOT NULL,
                "Description" text NULL,
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                "CreatedBy" text NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedBy" text NULL,
                "DeletedAt" timestamp with time zone NULL,
                "DeletedBy" text NULL,
                "IsDeleted" boolean NOT NULL DEFAULT FALSE
            );
            """);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_ReceivablePayables_TenantId_Type_Status_DueDate"
            ON "ReceivablePayables" ("TenantId", "Type", "Status", "DueDate");
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "ReceivablePayables" ADD COLUMN IF NOT EXISTS "LastSettlementDate" timestamp with time zone NULL;
            """);
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE IF EXISTS "Tenants" ADD COLUMN IF NOT EXISTS "IsTrial" boolean NOT NULL DEFAULT FALSE;
            """);
    }

    private static async Task NormalizeSharedLookupDataAsync(ApplicationDbContext context)
    {
        if (!await context.VehicleSegments.IgnoreQueryFilters().AnyAsync(item => item.TenantId == SharedLookupTenantId))
        {
            var segmentNames = await context.VehicleSegments.IgnoreQueryFilters()
                .Where(item => item.TenantId != SharedLookupTenantId && !item.IsDeleted)
                .Select(item => item.Name)
                .Distinct()
                .ToListAsync();

            if (segmentNames.Count > 0)
            {
                context.VehicleSegments.AddRange(segmentNames.Select(name => new VehicleSegment
                {
                    TenantId = SharedLookupTenantId,
                    Name = name
                }));
                await context.SaveChangesAsync();
            }
        }

        if (!await context.VehicleBrands.IgnoreQueryFilters().AnyAsync(item => item.TenantId == SharedLookupTenantId))
        {
            var brandNames = await context.VehicleBrands.IgnoreQueryFilters()
                .Where(item => item.TenantId != SharedLookupTenantId && !item.IsDeleted)
                .Select(item => item.Name)
                .Distinct()
                .ToListAsync();

            if (brandNames.Count > 0)
            {
                context.VehicleBrands.AddRange(brandNames.Select(name => new VehicleBrand
                {
                    TenantId = SharedLookupTenantId,
                    Name = name
                }));
                await context.SaveChangesAsync();
            }
        }

        if (!await context.VehicleCatalogModels.IgnoreQueryFilters().AnyAsync(item => item.TenantId == SharedLookupTenantId))
        {
            var sharedBrands = await context.VehicleBrands.IgnoreQueryFilters()
                .Where(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted)
                .ToDictionaryAsync(item => item.Name, item => item.Id);

            var sharedSegments = await context.VehicleSegments.IgnoreQueryFilters()
                .Where(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted)
                .ToDictionaryAsync(item => item.Name, item => item.Id);

            var sourceModels = await context.VehicleCatalogModels.IgnoreQueryFilters()
                .Where(item => item.TenantId != SharedLookupTenantId && !item.IsDeleted)
                .Select(item => new
                {
                    item.Name,
                    BrandName = item.VehicleBrand.Name,
                    SegmentName = item.VehicleSegment != null ? item.VehicleSegment.Name : null
                })
                .Distinct()
                .ToListAsync();

            foreach (var model in sourceModels)
            {
                if (!sharedBrands.TryGetValue(model.BrandName, out var brandId))
                {
                    continue;
                }

                Guid? segmentId = null;
                if (!string.IsNullOrWhiteSpace(model.SegmentName) && sharedSegments.TryGetValue(model.SegmentName, out var foundSegmentId))
                {
                    segmentId = foundSegmentId;
                }

                var exists = await context.VehicleCatalogModels.IgnoreQueryFilters().AnyAsync(item =>
                    item.TenantId == SharedLookupTenantId &&
                    !item.IsDeleted &&
                    item.Name == model.Name &&
                    item.VehicleBrandId == brandId &&
                    item.VehicleSegmentId == segmentId);

                if (!exists)
                {
                    context.VehicleCatalogModels.Add(new VehicleCatalogModel
                    {
                        TenantId = SharedLookupTenantId,
                        Name = model.Name,
                        VehicleBrandId = brandId,
                        VehicleSegmentId = segmentId
                    });
                }
            }

            await context.SaveChangesAsync();
        }

        if (!await context.VehicleExpenseCategories.IgnoreQueryFilters().AnyAsync(item => item.TenantId == SharedLookupTenantId))
        {
            var categoryNames = await context.ExpenseCategories.IgnoreQueryFilters()
                .Where(item => item.TenantId != SharedLookupTenantId && !item.IsDeleted)
                .Select(item => item.Name)
                .Distinct()
                .ToListAsync();

            if (categoryNames.Count > 0)
            {
                context.VehicleExpenseCategories.AddRange(categoryNames.Select(name => new VehicleExpenseCategory
                {
                    TenantId = SharedLookupTenantId,
                    Name = name
                }));
                await context.SaveChangesAsync();
            }
        }

        var legacySharedCategories = await context.ExpenseCategories.IgnoreQueryFilters()
            .Where(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted)
            .Select(item => new { item.Name, item.CategoryType })
            .ToListAsync();

        var existingVehicleCategories = await context.VehicleExpenseCategories.IgnoreQueryFilters()
            .Where(item => item.TenantId == SharedLookupTenantId)
            .ToListAsync();
        var existingGeneralCategories = await context.GeneralExpenseCategories.IgnoreQueryFilters()
            .Where(item => item.TenantId == SharedLookupTenantId)
            .ToListAsync();

        var vehicleFromLegacy = new List<VehicleExpenseCategory>();
        foreach (var name in legacySharedCategories
                     .Where(item => item.CategoryType == ExpenseCategoryType.Vehicle)
                     .Select(item => item.Name)
                     .Distinct())
        {
            var existing = existingVehicleCategories.FirstOrDefault(item => item.Name == name);
            if (existing != null)
            {
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.DeletedAt = null;
                    existing.DeletedBy = null;
                }

                continue;
            }

            vehicleFromLegacy.Add(new VehicleExpenseCategory
            {
                TenantId = SharedLookupTenantId,
                Name = name
            });
        }

        var generalFromLegacy = new List<GeneralExpenseCategory>();
        foreach (var name in legacySharedCategories
                     .Where(item => item.CategoryType == ExpenseCategoryType.General)
                     .Select(item => item.Name)
                     .Distinct())
        {
            var existing = existingGeneralCategories.FirstOrDefault(item => item.Name == name);
            if (existing != null)
            {
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.DeletedAt = null;
                    existing.DeletedBy = null;
                }

                continue;
            }

            generalFromLegacy.Add(new GeneralExpenseCategory
            {
                TenantId = SharedLookupTenantId,
                Name = name
            });
        }

        if (vehicleFromLegacy.Count > 0)
        {
            context.VehicleExpenseCategories.AddRange(vehicleFromLegacy);
        }

        if (generalFromLegacy.Count > 0)
        {
            context.GeneralExpenseCategories.AddRange(generalFromLegacy);
        }

        if (vehicleFromLegacy.Count > 0 || generalFromLegacy.Count > 0)
        {
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedVehicleLookupsAsync(ApplicationDbContext context)
    {
        if (await context.VehicleSegments.IgnoreQueryFilters().AnyAsync(item => item.TenantId == SharedLookupTenantId))
        {
            return;
        }

        var segments = new[] { "Sedan", "SUV", "Hatchback", "Coupe", "Pickup", "Van" };
        var brands = new[] { "BMW", "Mercedes-Benz", "Audi", "Volkswagen", "Renault", "Fiat", "Ford", "Toyota" };

        var segmentEntities = segments.Select(name => new VehicleSegment { TenantId = SharedLookupTenantId, Name = name }).ToList();
        var brandEntities = brands.Select(name => new VehicleBrand { TenantId = SharedLookupTenantId, Name = name }).ToList();

        context.VehicleSegments.AddRange(segmentEntities);
        context.VehicleBrands.AddRange(brandEntities);
        await context.SaveChangesAsync();

        var segmentMap = segmentEntities.ToDictionary(item => item.Name);
        var brandMap = brandEntities.ToDictionary(item => item.Name);

        var models = new (string Name, string Brand, string Segment)[]
        {
            ("320i", "BMW", "Sedan"),
            ("X5", "BMW", "SUV"),
            ("C180", "Mercedes-Benz", "Sedan"),
            ("GLC", "Mercedes-Benz", "SUV"),
            ("A4", "Audi", "Sedan"),
            ("Q5", "Audi", "SUV"),
            ("Passat", "Volkswagen", "Sedan"),
            ("Tiguan", "Volkswagen", "SUV"),
            ("Clio", "Renault", "Hatchback"),
            ("Megane", "Renault", "Sedan"),
            ("Egea", "Fiat", "Sedan"),
            ("Doblo", "Fiat", "Van"),
            ("Focus", "Ford", "Sedan"),
            ("Ranger", "Ford", "Pickup"),
            ("Corolla", "Toyota", "Sedan"),
            ("C-HR", "Toyota", "SUV")
        };

        context.VehicleCatalogModels.AddRange(models.Select(item => new VehicleCatalogModel
        {
            TenantId = SharedLookupTenantId,
            Name = item.Name,
            VehicleBrandId = brandMap[item.Brand].Id,
            VehicleSegmentId = segmentMap[item.Segment].Id
        }));

        await context.SaveChangesAsync();
    }

    private static async Task SeedExpenseCategoriesAsync(ApplicationDbContext context)
    {
        var vehicleDefaults = new[]
        {
            "Ekspertiz",
            "Bakim",
            "Tamir",
            "Temizlik",
            "Sigorta"
        };
        var generalDefaults = new[]
        {
            "Dukkan / Ofis Kirasi",
            "Elektrik / Su / Dogalgaz",
            "Internet / Telefon",
            "Aidat",
            "Guvenlik",
            "Temizlik Giderleri"
        };

        var existingVehicleCategories = await context.VehicleExpenseCategories.IgnoreQueryFilters()
            .Where(item => item.TenantId == SharedLookupTenantId)
            .ToListAsync();
        var existingGeneralCategories = await context.GeneralExpenseCategories.IgnoreQueryFilters()
            .Where(item => item.TenantId == SharedLookupTenantId)
            .ToListAsync();

        var vehicleToAdd = new List<VehicleExpenseCategory>();
        foreach (var name in vehicleDefaults)
        {
            var existing = existingVehicleCategories.FirstOrDefault(item => item.Name == name);
            if (existing != null)
            {
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.DeletedAt = null;
                    existing.DeletedBy = null;
                }

                continue;
            }

            vehicleToAdd.Add(new VehicleExpenseCategory
            {
                TenantId = SharedLookupTenantId,
                Name = name
            });
        }

        var generalToAdd = new List<GeneralExpenseCategory>();
        foreach (var name in generalDefaults)
        {
            var existing = existingGeneralCategories.FirstOrDefault(item => item.Name == name);
            if (existing != null)
            {
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.DeletedAt = null;
                    existing.DeletedBy = null;
                }

                continue;
            }

            generalToAdd.Add(new GeneralExpenseCategory
            {
                TenantId = SharedLookupTenantId,
                Name = name
            });
        }

        if (vehicleToAdd.Count == 0 && generalToAdd.Count == 0)
        {
            return;
        }

        context.VehicleExpenseCategories.AddRange(vehicleToAdd);
        context.GeneralExpenseCategories.AddRange(generalToAdd);

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
