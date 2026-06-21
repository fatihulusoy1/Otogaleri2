using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Subscription.Services;
using AutoGallerySaaS.Application.Features.TenantActivities.Services;
using AutoGallerySaaS.Application.Features.TenantUsers.Dtos;
using AutoGallerySaaS.Domain.Entities.Identity;
using AutoGallerySaaS.Domain.Entities.SaaS;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.TenantUsers.Services;

public class TenantUserService : ITenantUserService
{
    private const string TenantAdminRoleName = "TenantAdmin";

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISubscriptionLimitService _limit;
    private readonly ITenantActivityService _activityLog;

    public TenantUserService(
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

    public async Task<List<TenantUserDto>> GetUsersAsync()
    {
        var tenantId = CurrentTenantId();

        var users = await _context.Users.IgnoreQueryFilters()
            .Where(user => user.TenantId == tenantId && !user.IsDeleted)
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ToListAsync();

        var adminUserIds = await GetTenantAdminUserIdsAsync(tenantId);

        return users
            .Select(user => new TenantUserDto(
                user.Id,
                $"{user.FirstName} {user.LastName}".Trim(),
                user.FirstName,
                user.LastName,
                user.Email,
                user.IsActive,
                adminUserIds.Contains(user.Id),
                user.IsSuperAdmin,
                user.LastLoginAt))
            .ToList();
    }

    public async Task<TenantUserDto> CreateUserAsync(CreateTenantUserRequest request)
    {
        var tenantId = CurrentTenantId();

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
            .AnyAsync(user => user.TenantId == tenantId && user.Email.ToLower() == email && !user.IsDeleted);
        if (emailExists)
        {
            throw new Exception("Bu e-posta bu tenant'ta zaten kayıtlı.");
        }

        await _limit.EnsureCanAddUserAsync(tenantId);

        var user = new User
        {
            TenantId = tenantId,
            FirstName = (request.FirstName ?? string.Empty).Trim(),
            LastName = (request.LastName ?? string.Empty).Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true,
            IsSuperAdmin = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (request.IsTenantAdmin)
        {
            await GrantTenantAdminAsync(user.Id, tenantId);
        }

        await _activityLog.LogAsync(
            tenantId,
            TenantActivityType.UserCreated,
            $"Kullanıcı oluşturuldu: {email}{(request.IsTenantAdmin ? " (yönetici)" : string.Empty)}");

        return await BuildDtoAsync(user.Id, tenantId);
    }

    public async Task<TenantUserDto> UpdateUserAsync(Guid userId, UpdateTenantUserRequest request)
    {
        var tenantId = CurrentTenantId();
        var user = await LoadManageableUserAsync(userId, tenantId);

        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new Exception("E-posta zorunludur.");
        }

        var emailExists = await _context.Users.IgnoreQueryFilters()
            .AnyAsync(item => item.Id != userId && item.TenantId == tenantId && item.Email.ToLower() == email && !item.IsDeleted);
        if (emailExists)
        {
            throw new Exception("Bu e-posta bu tenant'ta zaten kayıtlı.");
        }

        user.FirstName = (request.FirstName ?? string.Empty).Trim();
        user.LastName = (request.LastName ?? string.Empty).Trim();
        user.Email = email;
        await _context.SaveChangesAsync();

        var currentlyAdmin = await IsTenantAdminAsync(userId, tenantId);
        if (request.IsTenantAdmin && !currentlyAdmin)
        {
            await GrantTenantAdminAsync(userId, tenantId);
            await _activityLog.LogAsync(tenantId, TenantActivityType.TenantAdminChanged, $"Yönetici yetkisi verildi: {email}");
        }
        else if (!request.IsTenantAdmin && currentlyAdmin)
        {
            if (!await HasOtherActiveTenantAdminAsync(tenantId, userId))
            {
                throw new Exception("Tenant'taki son yönetici yetkisi kaldırılamaz.");
            }

            await RevokeTenantAdminAsync(userId, tenantId);
            await _activityLog.LogAsync(tenantId, TenantActivityType.TenantAdminChanged, $"Yönetici yetkisi kaldırıldı: {email}");
        }

        return await BuildDtoAsync(userId, tenantId);
    }

    public async Task SetStatusAsync(Guid userId, SetTenantUserStatusRequest request)
    {
        var tenantId = CurrentTenantId();
        var user = await LoadManageableUserAsync(userId, tenantId);

        if (!request.IsActive)
        {
            if (userId == CurrentUserId())
            {
                throw new Exception("Kendinizi pasifleştiremezsiniz.");
            }

            if (await IsTenantAdminAsync(userId, tenantId) && !await HasOtherActiveTenantAdminAsync(tenantId, userId))
            {
                throw new Exception("Tenant'taki son aktif yönetici pasifleştirilemez.");
            }
        }

        user.IsActive = request.IsActive;
        if (!request.IsActive)
        {
            // Pasifleştirilen kullanıcının açık oturumları/refresh token'ı geçersiz kılınır.
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
        }
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(
            tenantId,
            TenantActivityType.UserStatusChanged,
            $"Kullanıcı {(request.IsActive ? "aktifleştirildi" : "pasifleştirildi")}: {user.Email}");
    }

    public async Task SetPasswordAsync(Guid userId, SetTenantUserPasswordRequest request)
    {
        var tenantId = CurrentTenantId();
        var user = await LoadManageableUserAsync(userId, tenantId);

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            throw new Exception("Şifre en az 6 karakter olmalıdır.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(tenantId, TenantActivityType.PasswordReset, $"Şifre sıfırlandı: {user.Email}");
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var tenantId = CurrentTenantId();

        if (userId == CurrentUserId())
        {
            throw new Exception("Kendi hesabınızı silemezsiniz.");
        }

        var user = await LoadManageableUserAsync(userId, tenantId);

        if (await IsTenantAdminAsync(userId, tenantId) && !await HasOtherActiveTenantAdminAsync(tenantId, userId))
        {
            throw new Exception("Tenant'taki son yönetici silinemez.");
        }

        user.IsDeleted = true;
        user.IsActive = false;
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(tenantId, TenantActivityType.UserDeleted, $"Kullanıcı silindi: {user.Email}");
    }

    private Guid CurrentTenantId()
    {
        return _currentUser.TenantId ?? throw new Exception("Aktif tenant bulunamadı.");
    }

    private Guid? CurrentUserId()
    {
        return Guid.TryParse(_currentUser.UserId, out var userId) ? userId : null;
    }

    private async Task<User> LoadManageableUserAsync(Guid userId, Guid tenantId)
    {
        var user = await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == userId && item.TenantId == tenantId && !item.IsDeleted);
        if (user == null)
        {
            throw new Exception("Kullanıcı bulunamadı.");
        }

        if (user.IsSuperAdmin)
        {
            throw new Exception("Bu kullanıcı bu ekrandan yönetilemez.");
        }

        return user;
    }

    private async Task<HashSet<Guid>> GetTenantAdminUserIdsAsync(Guid tenantId)
    {
        var ids = await _context.UserRoles.IgnoreQueryFilters()
            .Where(userRole => userRole.TenantId == tenantId && userRole.Role.Name == TenantAdminRoleName)
            .Select(userRole => userRole.UserId)
            .ToListAsync();
        return ids.ToHashSet();
    }

    private async Task<bool> IsTenantAdminAsync(Guid userId, Guid tenantId)
    {
        return await _context.UserRoles.IgnoreQueryFilters()
            .AnyAsync(userRole =>
                userRole.TenantId == tenantId &&
                userRole.UserId == userId &&
                userRole.Role.Name == TenantAdminRoleName);
    }

    private async Task<bool> HasOtherActiveTenantAdminAsync(Guid tenantId, Guid excludingUserId)
    {
        return await _context.UserRoles.IgnoreQueryFilters()
            .Where(userRole =>
                userRole.TenantId == tenantId &&
                userRole.Role.Name == TenantAdminRoleName &&
                userRole.UserId != excludingUserId)
            .Join(
                _context.Users.IgnoreQueryFilters(),
                userRole => userRole.UserId,
                user => user.Id,
                (userRole, user) => user)
            .AnyAsync(user => user.IsActive && !user.IsDeleted);
    }

    private async Task GrantTenantAdminAsync(Guid userId, Guid tenantId)
    {
        var role = await _context.Roles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.TenantId == tenantId && item.Name == TenantAdminRoleName);

        if (role == null)
        {
            role = new Role
            {
                TenantId = tenantId,
                Name = TenantAdminRoleName,
                Description = "Tenant administrator",
                IsStatic = true
            };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }

        var alreadyLinked = await _context.UserRoles.IgnoreQueryFilters()
            .AnyAsync(item => item.UserId == userId && item.RoleId == role.Id);
        if (!alreadyLinked)
        {
            _context.UserRoles.Add(new UserRole
            {
                TenantId = tenantId,
                UserId = userId,
                RoleId = role.Id
            });
            await _context.SaveChangesAsync();
        }
    }

    private async Task RevokeTenantAdminAsync(Guid userId, Guid tenantId)
    {
        var roleIds = await _context.Roles.IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId && item.Name == TenantAdminRoleName)
            .Select(item => item.Id)
            .ToListAsync();

        var links = await _context.UserRoles.IgnoreQueryFilters()
            .Where(item => item.UserId == userId && roleIds.Contains(item.RoleId))
            .ToListAsync();

        if (links.Count > 0)
        {
            _context.UserRoles.RemoveRange(links);
            await _context.SaveChangesAsync();
        }
    }

    private async Task<TenantUserDto> BuildDtoAsync(Guid userId, Guid tenantId)
    {
        var user = await _context.Users.IgnoreQueryFilters().FirstAsync(item => item.Id == userId);
        var isTenantAdmin = await IsTenantAdminAsync(userId, tenantId);

        return new TenantUserDto(
            user.Id,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.FirstName,
            user.LastName,
            user.Email,
            user.IsActive,
            isTenantAdmin,
            user.IsSuperAdmin,
            user.LastLoginAt);
    }
}
