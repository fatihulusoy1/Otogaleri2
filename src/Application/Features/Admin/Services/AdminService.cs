using AutoGallerySaaS.Application.Common;
using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Admin.Dtos;
using AutoGallerySaaS.Application.Features.Finance.Dtos;
using AutoGallerySaaS.Application.Features.Subscription.Services;
using AutoGallerySaaS.Application.Features.TenantActivities.Dtos;
using AutoGallerySaaS.Application.Features.TenantActivities.Services;
using AutoGallerySaaS.Domain.Entities.Finance;
using AutoGallerySaaS.Domain.Entities.Identity;
using AutoGallerySaaS.Domain.Entities.SaaS;
using AutoGallerySaaS.Domain.Entities.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.Admin.Services;

public class AdminService : IAdminService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISubscriptionLimitService _limit;
    private readonly ITenantActivityService _activityLog;
    private static readonly Guid SharedLookupTenantId = SharedTenantIds.Catalog;

    public AdminService(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ISubscriptionLimitService limit,
        ITenantActivityService activityLog)
    {
        _context = context;
        _currentUser = currentUser;
        _limit = limit;
        _activityLog = activityLog;
    }

    public async Task<List<AdminTenantDto>> GetTenantsAsync()
    {
        var tenants = await _context.Tenants
            .IgnoreQueryFilters()
            .Where(tenant => !tenant.IsDeleted)
            .OrderBy(tenant => tenant.Name)
            .Select(tenant => new AdminTenantDto(
                tenant.Id,
                tenant.Name,
                tenant.Identifier,
                tenant.IsActive,
                _context.Users.IgnoreQueryFilters().Count(user => user.TenantId == tenant.Id && !user.IsDeleted),
                tenant.SubscriptionPlanId,
                _context.SubscriptionPlans.IgnoreQueryFilters()
                    .Where(plan => plan.Id == tenant.SubscriptionPlanId)
                    .Select(plan => plan.Name)
                    .FirstOrDefault() ?? "-",
                tenant.SubscriptionEndDate))
            .ToListAsync();

        return tenants;
    }

    public async Task<List<AdminSubscriptionPlanDto>> GetSubscriptionPlansAsync()
    {
        return await _context.SubscriptionPlans
            .IgnoreQueryFilters()
            .OrderBy(plan => plan.MonthlyPrice)
            .Select(plan => new AdminSubscriptionPlanDto(
                plan.Id,
                plan.Name,
                plan.Description,
                plan.MonthlyPrice,
                plan.YearlyPrice,
                plan.MaxUsers,
                plan.MaxVehicles,
                plan.IsActive))
            .ToListAsync();
    }

    public async Task<AdminSubscriptionPlanDto> CreatePlanAsync(CreatePlanRequest request)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new Exception("Paket adı zorunludur.");
        }

        var exists = await _context.SubscriptionPlans.IgnoreQueryFilters().AnyAsync(plan => plan.Name == name);
        if (exists)
        {
            throw new Exception("Bu isimde bir paket zaten var.");
        }

        var plan = new SubscriptionPlan
        {
            Name = name,
            Description = (request.Description ?? string.Empty).Trim(),
            MonthlyPrice = request.MonthlyPrice,
            YearlyPrice = request.YearlyPrice,
            MaxUsers = request.MaxUsers,
            MaxVehicles = request.MaxVehicles,
            IsActive = request.IsActive
        };
        _context.SubscriptionPlans.Add(plan);
        await _context.SaveChangesAsync();

        return MapPlan(plan);
    }

    public async Task<AdminSubscriptionPlanDto> UpdatePlanAsync(Guid planId, UpdatePlanRequest request)
    {
        var plan = await _context.SubscriptionPlans.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == planId);
        if (plan == null)
        {
            throw new Exception("Paket bulunamadı.");
        }

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new Exception("Paket adı zorunludur.");
        }

        var nameTaken = await _context.SubscriptionPlans.IgnoreQueryFilters()
            .AnyAsync(item => item.Id != planId && item.Name == name);
        if (nameTaken)
        {
            throw new Exception("Bu isimde bir paket zaten var.");
        }

        // NOT: Burada yapılan değişiklik mevcut abonelerin geçerli dönemini ETKİLEMEZ.
        // Tenant'lar abone olurken/yenilerken limitleri snapshot olarak saklar; yeni değerler bir sonraki yenilemede uygulanır.
        plan.Name = name;
        plan.Description = (request.Description ?? string.Empty).Trim();
        plan.MonthlyPrice = request.MonthlyPrice;
        plan.YearlyPrice = request.YearlyPrice;
        plan.MaxUsers = request.MaxUsers;
        plan.MaxVehicles = request.MaxVehicles;
        plan.IsActive = request.IsActive;
        await _context.SaveChangesAsync();

        return MapPlan(plan);
    }

    public async Task DeletePlanAsync(Guid planId)
    {
        var plan = await _context.SubscriptionPlans.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == planId);
        if (plan == null)
        {
            return;
        }

        var inUse = await _context.Tenants.IgnoreQueryFilters()
            .AnyAsync(tenant => tenant.SubscriptionPlanId == planId && !tenant.IsDeleted);
        if (inUse)
        {
            throw new Exception("Bu paket bir veya daha fazla tenant tarafından kullanılıyor; silinemez. Pasif yapabilirsiniz.");
        }

        _context.SubscriptionPlans.Remove(plan);
        await _context.SaveChangesAsync();
    }

    private static AdminSubscriptionPlanDto MapPlan(SubscriptionPlan plan)
    {
        return new AdminSubscriptionPlanDto(
            plan.Id,
            plan.Name,
            plan.Description,
            plan.MonthlyPrice,
            plan.YearlyPrice,
            plan.MaxUsers,
            plan.MaxVehicles,
            plan.IsActive);
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
                item.user.IsSuperAdmin,
                item.user.LastLoginAt))
            .ToListAsync();
    }

    public async Task UpdateTenantStatusAsync(Guid tenantId, bool isActive)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == tenantId);
        if (tenant == null)
        {
            throw new Exception("Tenant not found");
        }

        tenant.IsActive = isActive;
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(tenant.Id, TenantActivityType.TenantStatusChanged, $"Tenant {(isActive ? "aktifleştirildi" : "pasifleştirildi")}: {tenant.Name}");
    }

    public async Task UpdateUserStatusAsync(Guid userId, bool isActive)
    {
        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }

        user.IsActive = isActive;
        if (!isActive)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
        }
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(user.TenantId, TenantActivityType.UserStatusChanged, $"Kullanıcı {(isActive ? "aktifleştirildi" : "pasifleştirildi")}: {user.Email}");
    }

    public async Task<AdminTenantDto> CreateTenantAsync(CreateTenantRequest request)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new Exception("Tenant adı zorunludur.");
        }

        var adminEmail = (request.AdminEmail ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            throw new Exception("Yönetici e-postası zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(request.AdminPassword) || request.AdminPassword.Length < 6)
        {
            throw new Exception("Şifre en az 6 karakter olmalıdır.");
        }

        var emailExists = await _context.Users.IgnoreQueryFilters().AnyAsync(item => item.Email == adminEmail);
        if (emailExists)
        {
            throw new Exception("Bu e-posta zaten kayıtlı.");
        }

        var plan = await _context.SubscriptionPlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == request.SubscriptionPlanId);
        if (plan == null)
        {
            throw new Exception("Abonelik paketi bulunamadı.");
        }

        var identifier = await BuildTenantIdentifierAsync(string.IsNullOrWhiteSpace(request.Identifier) ? name : request.Identifier!);

        var tenant = new Tenant
        {
            Name = name,
            Identifier = identifier,
            SubscriptionPlanId = plan.Id,
            SubscriptionEndDate = DateTime.SpecifyKind(request.SubscriptionEndDate, DateTimeKind.Utc),
            EffectiveMaxUsers = plan.MaxUsers,
            EffectiveMaxVehicles = plan.MaxVehicles,
            IsActive = true
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var user = new User
        {
            TenantId = tenant.Id,
            FirstName = (request.AdminFirstName ?? string.Empty).Trim(),
            LastName = (request.AdminLastName ?? string.Empty).Trim(),
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword),
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var adminRole = new Role
        {
            TenantId = tenant.Id,
            Name = "TenantAdmin",
            Description = "Tenant administrator",
            IsStatic = true
        };
        _context.Roles.Add(adminRole);
        await _context.SaveChangesAsync();

        var permissionIds = await _context.Permissions.IgnoreQueryFilters()
            .Select(permission => permission.Id)
            .ToListAsync();

        _context.UserRoles.Add(new UserRole
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            RoleId = adminRole.Id
        });
        _context.RolePermissions.AddRange(permissionIds.Select(permissionId => new RolePermission
        {
            TenantId = tenant.Id,
            RoleId = adminRole.Id,
            PermissionId = permissionId
        }));
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(tenant.Id, TenantActivityType.TenantCreated, $"Tenant oluşturuldu: {tenant.Name} ({plan.Name})");

        return new AdminTenantDto(tenant.Id, tenant.Name, tenant.Identifier, tenant.IsActive, 1, plan.Id, plan.Name, tenant.SubscriptionEndDate);
    }

    public async Task<AdminTenantDto> UpdateTenantAsync(Guid tenantId, UpdateTenantRequest request)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == tenantId);
        if (tenant == null)
        {
            throw new Exception("Tenant bulunamadı.");
        }

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new Exception("Tenant adı zorunludur.");
        }

        var plan = await _context.SubscriptionPlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == request.SubscriptionPlanId);
        if (plan == null)
        {
            throw new Exception("Abonelik paketi bulunamadı.");
        }

        var requestedIdentifier = string.IsNullOrWhiteSpace(request.Identifier) ? null : request.Identifier!.Trim();
        if (requestedIdentifier != null && requestedIdentifier != tenant.Identifier)
        {
            tenant.Identifier = await BuildTenantIdentifierAsync(requestedIdentifier, tenant.Id);
        }
        else if (requestedIdentifier == null)
        {
            tenant.Identifier = null;
        }

        tenant.Name = name;
        tenant.SubscriptionPlanId = plan.Id;
        tenant.SubscriptionEndDate = DateTime.SpecifyKind(request.SubscriptionEndDate, DateTimeKind.Utc);
        // Super admin paketi elle değiştirdiğinde yeni plan limitlerini snapshot'a yansıt.
        tenant.EffectiveMaxUsers = plan.MaxUsers;
        tenant.EffectiveMaxVehicles = plan.MaxVehicles;
        tenant.IsActive = request.IsActive;
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(tenant.Id, TenantActivityType.TenantUpdated, $"Tenant güncellendi: {tenant.Name} ({plan.Name})");

        var userCount = await _context.Users.IgnoreQueryFilters().CountAsync(item => item.TenantId == tenant.Id && !item.IsDeleted);
        return new AdminTenantDto(tenant.Id, tenant.Name, tenant.Identifier, tenant.IsActive, userCount, plan.Id, plan.Name, tenant.SubscriptionEndDate);
    }

    public async Task DeleteTenantAsync(Guid tenantId)
    {
        if (_currentUser.TenantId == tenantId)
        {
            throw new Exception("Kendi bağlı olduğunuz tenant silinemez.");
        }

        var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == tenantId);
        if (tenant == null || tenant.IsDeleted)
        {
            return;
        }

        var hasSuperAdmin = await _context.Users.IgnoreQueryFilters()
            .AnyAsync(item => item.TenantId == tenantId && item.IsSuperAdmin && !item.IsDeleted);
        if (hasSuperAdmin)
        {
            throw new Exception("Bu tenant silinemez: içinde super admin kullanıcı(lar) bulunuyor. Önce super admin'i başka bir tenant'a taşıyın veya super admin yetkisini kaldırın.");
        }

        var users = await _context.Users.IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId && !item.IsDeleted)
            .ToListAsync();
        foreach (var user in users)
        {
            user.IsDeleted = true;
            user.IsActive = false;
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
        }

        tenant.IsDeleted = true;
        tenant.IsActive = false;
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(tenantId, TenantActivityType.TenantDeleted, $"Tenant silindi: {tenant.Name}");
    }

    public Task<List<TenantActivityDto>> GetTenantActivitiesAsync(Guid tenantId)
    {
        return _activityLog.GetActivitiesForTenantAsync(tenantId);
    }

    public async Task<AdminUserDto> CreateUserAsync(CreateUserRequest request)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == request.TenantId && !item.IsDeleted);
        if (tenant == null)
        {
            throw new Exception("Tenant bulunamadı.");
        }

        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new Exception("E-posta zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            throw new Exception("Şifre en az 6 karakter olmalıdır.");
        }

        var emailExists = await _context.Users.IgnoreQueryFilters()
            .AnyAsync(item => item.TenantId == tenant.Id && item.Email.ToLower() == email && !item.IsDeleted);
        if (emailExists)
        {
            throw new Exception("Bu e-posta bu tenant'ta zaten kayıtlı.");
        }

        // Super admin de oluştururken tenant'ın paket limitine uyar (super admin kullanıcılar limit dışı).
        if (!request.IsSuperAdmin)
        {
            await _limit.EnsureCanAddUserAsync(tenant.Id);
        }

        var user = new User
        {
            TenantId = tenant.Id,
            FirstName = (request.FirstName ?? string.Empty).Trim(),
            LastName = (request.LastName ?? string.Empty).Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true,
            IsSuperAdmin = request.IsSuperAdmin
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(
            tenant.Id,
            TenantActivityType.UserCreated,
            $"Kullanıcı oluşturuldu (super admin tarafından): {email}");

        return new AdminUserDto(
            user.Id,
            user.TenantId,
            tenant.Name,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.Email,
            user.IsActive,
            user.IsSuperAdmin,
            user.LastLoginAt);
    }

    public async Task<AdminUserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == userId && !item.IsDeleted);
        if (user == null)
        {
            throw new Exception("Kullanıcı bulunamadı.");
        }

        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new Exception("E-posta zorunludur.");
        }

        var emailExists = await _context.Users.IgnoreQueryFilters()
            .AnyAsync(item => item.Id != userId && item.TenantId == user.TenantId && item.Email.ToLower() == email && !item.IsDeleted);
        if (emailExists)
        {
            throw new Exception("Bu e-posta bu tenant'ta zaten kayıtlı.");
        }

        if (user.IsSuperAdmin && !request.IsSuperAdmin && await IsLastActiveSuperAdminAsync(userId))
        {
            throw new Exception("Sistemdeki son aktif super admin yetkisi kaldırılamaz.");
        }

        user.FirstName = (request.FirstName ?? string.Empty).Trim();
        user.LastName = (request.LastName ?? string.Empty).Trim();
        user.Email = email;
        user.IsActive = request.IsActive;
        user.IsSuperAdmin = request.IsSuperAdmin;
        if (!request.IsActive)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
        }
        await _context.SaveChangesAsync();

        var tenantName = await _context.Tenants.IgnoreQueryFilters()
            .Where(item => item.Id == user.TenantId)
            .Select(item => item.Name)
            .FirstOrDefaultAsync() ?? "-";

        return new AdminUserDto(
            user.Id,
            user.TenantId,
            tenantName,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.Email,
            user.IsActive,
            user.IsSuperAdmin,
            user.LastLoginAt);
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        if (Guid.TryParse(_currentUser.UserId, out var currentUserId) && currentUserId == userId)
        {
            throw new Exception("Kendi hesabınızı silemezsiniz.");
        }

        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == userId);
        if (user == null || user.IsDeleted)
        {
            return;
        }

        if (user.IsSuperAdmin && await IsLastActiveSuperAdminAsync(userId))
        {
            throw new Exception("Sistemdeki son aktif super admin silinemez.");
        }

        user.IsDeleted = true;
        user.IsActive = false;
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _context.SaveChangesAsync();
    }

    public async Task SetUserPasswordAsync(Guid userId, SetUserPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            throw new Exception("Şifre en az 6 karakter olmalıdır.");
        }

        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == userId && !item.IsDeleted);
        if (user == null)
        {
            throw new Exception("Kullanıcı bulunamadı.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(user.TenantId, TenantActivityType.PasswordReset, $"Şifre sıfırlandı (super admin): {user.Email}");
    }

    private async Task<bool> IsLastActiveSuperAdminAsync(Guid excludingUserId)
    {
        var otherSuperAdmins = await _context.Users.IgnoreQueryFilters()
            .CountAsync(item => item.Id != excludingUserId && item.IsSuperAdmin && item.IsActive && !item.IsDeleted);
        return otherSuperAdmins == 0;
    }

    private async Task<string> BuildTenantIdentifierAsync(string source, Guid? excludingTenantId = null)
    {
        var slugBase = new string(source
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray())
            .Trim('-');

        if (string.IsNullOrWhiteSpace(slugBase))
        {
            slugBase = "tenant";
        }

        var identifier = slugBase;
        var suffix = 1;
        while (await _context.Tenants.IgnoreQueryFilters()
            .AnyAsync(item => item.Identifier == identifier && (!excludingTenantId.HasValue || item.Id != excludingTenantId.Value)))
        {
            identifier = $"{slugBase}-{suffix++}";
        }

        return identifier;
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
            throw new Exception("Segment name is required");
        }

        var exists = await _context.VehicleSegments.IgnoreQueryFilters()
            .AnyAsync(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted && item.Name == name);
        if (exists)
        {
            throw new Exception("Segment already exists");
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
            throw new Exception("Segment not found");
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new Exception("Segment name is required");
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
            throw new Exception("Brand name is required");
        }

        var exists = await _context.VehicleBrands.IgnoreQueryFilters()
            .AnyAsync(item => item.TenantId == SharedLookupTenantId && !item.IsDeleted && item.Name == name);
        if (exists)
        {
            throw new Exception("Brand already exists");
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
            throw new Exception("Brand not found");
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new Exception("Brand name is required");
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
            throw new Exception("Model name is required");
        }

        var brand = await _context.VehicleBrands.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == request.BrandId && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
        if (brand == null)
        {
            throw new Exception("Brand not found");
        }

        if (request.SegmentId.HasValue)
        {
            var segmentExists = await _context.VehicleSegments.IgnoreQueryFilters()
                .AnyAsync(item => item.Id == request.SegmentId.Value && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
            if (!segmentExists)
            {
                throw new Exception("Segment not found");
            }
        }

        var exists = await _context.VehicleCatalogModels.IgnoreQueryFilters()
            .AnyAsync(item =>
                item.TenantId == SharedLookupTenantId &&
                !item.IsDeleted &&
                item.VehicleBrandId == request.BrandId &&
                item.VehicleSegmentId == request.SegmentId &&
                item.Name == name);
        if (exists)
        {
            throw new Exception("Model already exists");
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
            throw new Exception("Model not found");
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
            throw new Exception("Expense category name is required");
        }

        if (request.CategoryType == ExpenseCategoryType.General)
        {
            var existingCategory = await _context.GeneralExpenseCategories.IgnoreQueryFilters()
                .FirstOrDefaultAsync(item => item.TenantId == SharedLookupTenantId && item.Name == name);

            if (existingCategory != null)
            {
                if (!existingCategory.IsDeleted)
                {
                    throw new Exception("Expense category already exists");
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
                throw new Exception("Expense category already exists");
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
            throw new Exception("Expense category name is required");
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
                    throw new Exception("Expense category already exists");
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
            throw new Exception("Expense category not found");
        }

        var generalConflicting = await _context.GeneralExpenseCategories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id != id && item.TenantId == SharedLookupTenantId && item.Name == name);
        if (generalConflicting != null)
        {
            if (!generalConflicting.IsDeleted)
            {
                throw new Exception("Expense category already exists");
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
            throw new Exception("Model name is required");
        }

        var brand = await _context.VehicleBrands.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == request.BrandId && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
        if (brand == null)
        {
            throw new Exception("Brand not found");
        }

        if (request.SegmentId.HasValue)
        {
            var segmentExists = await _context.VehicleSegments.IgnoreQueryFilters()
                .AnyAsync(item => item.Id == request.SegmentId.Value && item.TenantId == SharedLookupTenantId && !item.IsDeleted);
            if (!segmentExists)
            {
                throw new Exception("Segment not found");
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
