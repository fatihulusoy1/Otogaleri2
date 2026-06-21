using AutoGallerySaaS.Application.Common;
using AutoGallerySaaS.Application.Common.Exceptions;
using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Admin.Dtos;
using AutoGallerySaaS.Application.Features.Finance.Dtos;
using AutoGallerySaaS.Domain.Entities.Finance;
using AutoGallerySaaS.Domain.Entities.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.Admin.Services;

public class AdminService : IAdminService
{
    private readonly IApplicationDbContext _context;
    private static readonly Guid SharedLookupTenantId = SharedTenantIds.Catalog;

    public AdminService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AdminTenantDto>> GetTenantsAsync()
    {
        var tenants = await _context.Tenants
            .IgnoreQueryFilters()
            .OrderBy(tenant => tenant.Name)
            .Select(tenant => new AdminTenantDto(
                tenant.Id,
                tenant.Name,
                tenant.Identifier,
                tenant.IsActive,
                _context.Users.IgnoreQueryFilters().Count(user => user.TenantId == tenant.Id && !user.IsDeleted)))
            .ToListAsync();

        return tenants;
    }

    public async Task<List<AdminUserDto>> GetUsersAsync(Guid? tenantId = null)
    {
        var query = _context.Users
            .IgnoreQueryFilters()
            .Join(
                _context.Tenants.IgnoreQueryFilters(),
                user => user.TenantId,
                tenant => tenant.Id,
                (user, tenant) => new { user, tenant })
            .Where(item => !item.user.IsDeleted);

        if (tenantId.HasValue)
        {
            query = query.Where(item => item.user.TenantId == tenantId.Value);
        }

        return await query
            .OrderBy(item => item.tenant.Name)
            .ThenBy(item => item.user.FirstName)
            .Select(item => new AdminUserDto(
                item.user.Id,
                item.user.TenantId,
                item.tenant.Name,
                $"{item.user.FirstName} {item.user.LastName}".Trim(),
                item.user.Email,
                item.user.IsActive,
                item.user.IsSuperAdmin))
            .ToListAsync();
    }

    public async Task UpdateTenantStatusAsync(Guid tenantId, bool isActive)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == tenantId);
        if (tenant == null)
        {
            throw new NotFoundException("Tenant not found");
        }

        tenant.IsActive = isActive;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateUserStatusAsync(Guid userId, bool isActive)
    {
        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == userId);
        if (user == null)
        {
            throw new NotFoundException("User not found");
        }

        user.IsActive = isActive;
        await _context.SaveChangesAsync();
    }

    public async Task<AdminCatalogLookupsDto> GetCatalogAsync()
    {
        await EnsureVehicleLookupsAsync();

        var segments = await _context.VehicleSegments
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted)
            .OrderBy(item => item.Name)
            .Select(item => new AdminLookupDto(item.Id, item.Name))
            .ToListAsync();

        var brands = await _context.VehicleBrands
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted)
            .OrderBy(item => item.Name)
            .Select(item => new AdminLookupDto(item.Id, item.Name))
            .ToListAsync();

        var models = await _context.VehicleCatalogModels
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted)
            .OrderBy(item => item.Name)
            .Select(item => new AdminCatalogModelDto(item.Id, item.Name, item.VehicleBrandId, item.VehicleSegmentId))
            .ToListAsync();

        return new AdminCatalogLookupsDto(segments, brands, models);
    }

    public async Task<AdminLookupDto> CreateSegmentAsync(CreateAdminSegmentRequest request)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Segment name is required");
        }

        var existing = await _context.VehicleSegments.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.TenantId == SharedLookupTenantId && item.Name == name);
        if (existing != null)
        {
            if (!existing.IsDeleted)
            {
                throw new BusinessRuleException("Segment already exists");
            }

            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.DeletedBy = null;
            await _context.SaveChangesAsync();
            return new AdminLookupDto(existing.Id, existing.Name);
        }

        var segment = new VehicleSegment
        {
            TenantId = SharedLookupTenantId,
            Name = name
        };
        _context.VehicleSegments.Add(segment);
        await _context.SaveChangesAsync();
        return new AdminLookupDto(segment.Id, segment.Name);
    }

    public async Task<AdminLookupDto> UpdateSegmentAsync(Guid id, UpdateNameRequest request)
    {
        var segment = await _context.VehicleSegments.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == id && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
        if (segment == null)
        {
            throw new NotFoundException("Segment not found");
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Segment name is required");
        }

        segment.Name = name;
        await _context.SaveChangesAsync();
        return new AdminLookupDto(segment.Id, segment.Name);
    }

    public async Task DeleteSegmentAsync(Guid id)
    {
        var segment = await _context.VehicleSegments.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == id && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
        if (segment == null)
        {
            return;
        }

        segment.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<AdminLookupDto> CreateBrandAsync(CreateAdminBrandRequest request)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Brand name is required");
        }

        var existing = await _context.VehicleBrands.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.TenantId == SharedLookupTenantId && item.Name == name);
        if (existing != null)
        {
            if (!existing.IsDeleted)
            {
                throw new BusinessRuleException("Brand already exists");
            }

            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.DeletedBy = null;
            await _context.SaveChangesAsync();
            return new AdminLookupDto(existing.Id, existing.Name);
        }

        var brand = new VehicleBrand
        {
            TenantId = SharedLookupTenantId,
            Name = name
        };
        _context.VehicleBrands.Add(brand);
        await _context.SaveChangesAsync();
        return new AdminLookupDto(brand.Id, brand.Name);
    }

    public async Task<AdminLookupDto> UpdateBrandAsync(Guid id, UpdateNameRequest request)
    {
        var brand = await _context.VehicleBrands.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == id && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
        if (brand == null)
        {
            throw new NotFoundException("Brand not found");
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Brand name is required");
        }

        brand.Name = name;
        await _context.SaveChangesAsync();
        return new AdminLookupDto(brand.Id, brand.Name);
    }

    public async Task DeleteBrandAsync(Guid id)
    {
        var brand = await _context.VehicleBrands.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == id && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
        if (brand == null)
        {
            return;
        }

        brand.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<AdminCatalogModelDto> CreateModelAsync(CreateAdminModelRequest request)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Model name is required");
        }

        var brand = await _context.VehicleBrands.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == request.BrandId && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
        if (brand == null)
        {
            throw new NotFoundException("Brand not found");
        }

        if (request.SegmentId.HasValue)
        {
            var segmentExists = await _context.VehicleSegments.IgnoreQueryFilters()
                .AnyAsync(item => item.Id == request.SegmentId.Value && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
            if (!segmentExists)
            {
                throw new NotFoundException("Segment not found");
            }
        }

        var existing = await _context.VehicleCatalogModels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item =>
                item.TenantId == SharedLookupTenantId &&
                item.VehicleBrandId == request.BrandId &&
                item.VehicleSegmentId == request.SegmentId &&
                item.Name == name);
        if (existing != null)
        {
            if (!existing.IsDeleted)
            {
                throw new BusinessRuleException("Model already exists");
            }

            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.DeletedBy = null;
            await _context.SaveChangesAsync();
            return new AdminCatalogModelDto(existing.Id, existing.Name, existing.VehicleBrandId, existing.VehicleSegmentId);
        }

        var model = new VehicleCatalogModel
        {
            TenantId = SharedLookupTenantId,
            Name = name,
            VehicleBrandId = request.BrandId,
            VehicleSegmentId = request.SegmentId
        };
        _context.VehicleCatalogModels.Add(model);
        await _context.SaveChangesAsync();

        return new AdminCatalogModelDto(model.Id, model.Name, model.VehicleBrandId, model.VehicleSegmentId);
    }

    public async Task<AdminCatalogModelDto> UpdateModelAsync(Guid id, CreateAdminModelRequest request)
    {
        var model = await _context.VehicleCatalogModels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == id && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
        if (model == null)
        {
            throw new NotFoundException("Model not found");
        }

        var updated = await CreateOrValidateModelPayloadAsync(request);
        model.Name = updated.Name;
        model.VehicleBrandId = updated.BrandId;
        model.VehicleSegmentId = updated.SegmentId;
        await _context.SaveChangesAsync();

        return new AdminCatalogModelDto(model.Id, model.Name, model.VehicleBrandId, model.VehicleSegmentId);
    }

    public async Task DeleteModelAsync(Guid id)
    {
        var model = await _context.VehicleCatalogModels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == id && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
        if (model == null)
        {
            return;
        }

        model.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<List<ExpenseCategoryDto>> GetExpenseCategoriesAsync(ExpenseCategoryType? categoryType = null)
    {
        await EnsureExpenseCategoriesAsync();

        if (categoryType == ExpenseCategoryType.Vehicle)
        {
            return await _context.VehicleExpenseCategories
                .IgnoreQueryFilters()
                .Where(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted)
                .OrderBy(item => item.Name)
                .Select(item => new ExpenseCategoryDto(item.Id, item.Name, ExpenseCategoryType.Vehicle))
                .ToListAsync();
        }

        if (categoryType == ExpenseCategoryType.General)
        {
            return await _context.GeneralExpenseCategories
                .IgnoreQueryFilters()
                .Where(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted)
                .OrderBy(item => item.Name)
                .Select(item => new ExpenseCategoryDto(item.Id, item.Name, ExpenseCategoryType.General))
                .ToListAsync();
        }

        var vehicleCategories = await _context.VehicleExpenseCategories
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted)
            .Select(item => new ExpenseCategoryDto(item.Id, item.Name, ExpenseCategoryType.Vehicle))
            .ToListAsync();

        var generalCategories = await _context.GeneralExpenseCategories
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted)
            .Select(item => new ExpenseCategoryDto(item.Id, item.Name, ExpenseCategoryType.General))
            .ToListAsync();

        return vehicleCategories.Concat(generalCategories).OrderBy(item => item.Name).ToList();
    }

    public async Task<ExpenseCategoryDto> CreateExpenseCategoryAsync(CreateExpenseCategoryRequest request)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Expense category name is required");
        }

        if (request.CategoryType == ExpenseCategoryType.General)
        {
            var existingCategory = await _context.GeneralExpenseCategories.IgnoreQueryFilters()
                .FirstOrDefaultAsync(item => item.TenantId == SharedLookupTenantId && item.Name == name);

            if (existingCategory != null)
            {
                if (!existingCategory.IsDeleted)
                {
                    throw new BusinessRuleException("Expense category already exists");
                }

                existingCategory.IsDeleted = false;
                existingCategory.DeletedAt = null;
                existingCategory.DeletedBy = null;
                await _context.SaveChangesAsync();
                return new ExpenseCategoryDto(existingCategory.Id, existingCategory.Name, ExpenseCategoryType.General);
            }

            var category = new GeneralExpenseCategory
            {
                TenantId = SharedLookupTenantId,
                Name = name
            };
            _context.GeneralExpenseCategories.Add(category);
            await _context.SaveChangesAsync();
            return new ExpenseCategoryDto(category.Id, category.Name, ExpenseCategoryType.General);
        }

        var existingVehicleCategory = await _context.VehicleExpenseCategories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.TenantId == SharedLookupTenantId && item.Name == name);

        if (existingVehicleCategory != null)
        {
            if (!existingVehicleCategory.IsDeleted)
            {
                throw new BusinessRuleException("Expense category already exists");
            }

            existingVehicleCategory.IsDeleted = false;
            existingVehicleCategory.DeletedAt = null;
            existingVehicleCategory.DeletedBy = null;
            await _context.SaveChangesAsync();
            return new ExpenseCategoryDto(existingVehicleCategory.Id, existingVehicleCategory.Name, ExpenseCategoryType.Vehicle);
        }

        var vehicleCategory = new VehicleExpenseCategory
        {
            TenantId = SharedLookupTenantId,
            Name = name
        };
        _context.VehicleExpenseCategories.Add(vehicleCategory);
        await _context.SaveChangesAsync();

        return new ExpenseCategoryDto(vehicleCategory.Id, vehicleCategory.Name, ExpenseCategoryType.Vehicle);
    }

    public async Task<ExpenseCategoryDto> UpdateExpenseCategoryAsync(Guid id, UpdateNameRequest request)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Expense category name is required");
        }

        var vehicleCategory = await _context.VehicleExpenseCategories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == id && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
        if (vehicleCategory != null)
        {
            var conflicting = await _context.VehicleExpenseCategories.IgnoreQueryFilters()
                .FirstOrDefaultAsync(item => item.Id != id && item.TenantId == SharedLookupTenantId && item.Name == name);
            if (conflicting != null)
            {
                if (!conflicting.IsDeleted)
                {
                    throw new BusinessRuleException("Expense category already exists");
                }

                conflicting.IsDeleted = false;
                conflicting.DeletedAt = null;
                conflicting.DeletedBy = null;
                vehicleCategory.IsDeleted = true;
                await _context.SaveChangesAsync();
                return new ExpenseCategoryDto(conflicting.Id, conflicting.Name, ExpenseCategoryType.Vehicle);
            }

            vehicleCategory.Name = name;
            await _context.SaveChangesAsync();
            return new ExpenseCategoryDto(vehicleCategory.Id, vehicleCategory.Name, ExpenseCategoryType.Vehicle);
        }

        var generalCategory = await _context.GeneralExpenseCategories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == id && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
        if (generalCategory == null)
        {
            throw new NotFoundException("Expense category not found");
        }

        var generalConflicting = await _context.GeneralExpenseCategories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id != id && item.TenantId == SharedLookupTenantId && item.Name == name);
        if (generalConflicting != null)
        {
            if (!generalConflicting.IsDeleted)
            {
                throw new BusinessRuleException("Expense category already exists");
            }

            generalConflicting.IsDeleted = false;
            generalConflicting.DeletedAt = null;
            generalConflicting.DeletedBy = null;
            generalCategory.IsDeleted = true;
            await _context.SaveChangesAsync();
            return new ExpenseCategoryDto(generalConflicting.Id, generalConflicting.Name, ExpenseCategoryType.General);
        }

        generalCategory.Name = name;
        await _context.SaveChangesAsync();
        return new ExpenseCategoryDto(generalCategory.Id, generalCategory.Name, ExpenseCategoryType.General);
    }

    public async Task DeleteExpenseCategoryAsync(Guid id)
    {
        var vehicleCategory = await _context.VehicleExpenseCategories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == id && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
        if (vehicleCategory != null)
        {
            vehicleCategory.IsDeleted = true;
            await _context.SaveChangesAsync();
            return;
        }

        var generalCategory = await _context.GeneralExpenseCategories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == id && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
        if (generalCategory == null)
        {
            return;
        }

        generalCategory.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    private async Task EnsureVehicleLookupsAsync()
    {
        if (await _context.VehicleSegments.IgnoreQueryFilters().AnyAsync(item => item.TenantId == SharedLookupTenantId))
        {
            return;
        }

        var segments = new[] { "Sedan", "SUV", "Hatchback", "Coupe", "Pickup", "Van" };
        var brands = new[] { "BMW", "Mercedes-Benz", "Audi", "Volkswagen", "Renault", "Fiat", "Ford", "Toyota" };

        var segmentEntities = segments.Select(name => new VehicleSegment { TenantId = SharedLookupTenantId, Name = name }).ToList();
        var brandEntities = brands.Select(name => new VehicleBrand { TenantId = SharedLookupTenantId, Name = name }).ToList();

        _context.VehicleSegments.AddRange(segmentEntities);
        _context.VehicleBrands.AddRange(brandEntities);
        await _context.SaveChangesAsync();

        var segmentMap = segmentEntities.ToDictionary(item => item.Name);
        var brandMap = brandEntities.ToDictionary(item => item.Name);
        var models = new (string Name, string Brand, string Segment)[]
        {
            ("320i", "BMW", "Sedan"), ("X5", "BMW", "SUV"), ("C180", "Mercedes-Benz", "Sedan"),
            ("GLC", "Mercedes-Benz", "SUV"), ("A4", "Audi", "Sedan"), ("Q5", "Audi", "SUV"),
            ("Passat", "Volkswagen", "Sedan"), ("Tiguan", "Volkswagen", "SUV"), ("Clio", "Renault", "Hatchback"),
            ("Megane", "Renault", "Sedan"), ("Egea", "Fiat", "Sedan"), ("Doblo", "Fiat", "Van"),
            ("Focus", "Ford", "Sedan"), ("Ranger", "Ford", "Pickup"), ("Corolla", "Toyota", "Sedan"),
            ("C-HR", "Toyota", "SUV")
        };

        _context.VehicleCatalogModels.AddRange(models.Select(item => new VehicleCatalogModel
        {
            TenantId = SharedLookupTenantId,
            Name = item.Name,
            VehicleBrandId = brandMap[item.Brand].Id,
            VehicleSegmentId = segmentMap[item.Segment].Id
        }));

        await _context.SaveChangesAsync();
    }

    private async Task<(string Name, Guid BrandId, Guid? SegmentId)> CreateOrValidateModelPayloadAsync(CreateAdminModelRequest request)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Model name is required");
        }

        var brand = await _context.VehicleBrands.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == request.BrandId && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
        if (brand == null)
        {
            throw new NotFoundException("Brand not found");
        }

        if (request.SegmentId.HasValue)
        {
            var segmentExists = await _context.VehicleSegments.IgnoreQueryFilters()
                .AnyAsync(item => item.Id == request.SegmentId.Value && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
            if (!segmentExists)
            {
                throw new NotFoundException("Segment not found");
            }
        }

        return (name, request.BrandId, request.SegmentId);
    }

    private async Task EnsureExpenseCategoriesAsync()
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

        var existingVehicleNames = await _context.VehicleExpenseCategories.IgnoreQueryFilters()
            .Where(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted)
            .Select(item => item.Name)
            .ToListAsync();
        var existingGeneralNames = await _context.GeneralExpenseCategories.IgnoreQueryFilters()
            .Where(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted)
            .Select(item => item.Name)
            .ToListAsync();

        var vehicleToAdd = vehicleDefaults
            .Where(name => !existingVehicleNames.Contains(name))
            .Select(name => new VehicleExpenseCategory
            {
                TenantId = SharedLookupTenantId,
                Name = name
            })
            .ToList();
        var generalToAdd = generalDefaults
            .Where(name => !existingGeneralNames.Contains(name))
            .Select(name => new GeneralExpenseCategory
            {
                TenantId = SharedLookupTenantId,
                Name = name
            })
            .ToList();

        if (vehicleToAdd.Count == 0 && generalToAdd.Count == 0)
        {
            return;
        }

        _context.VehicleExpenseCategories.AddRange(vehicleToAdd);
        _context.GeneralExpenseCategories.AddRange(generalToAdd);

        await _context.SaveChangesAsync();
    }
}
