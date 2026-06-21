using AutoGallerySaaS.Application.Common.Exceptions;
using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Auth.Dtos;
using AutoGallerySaaS.Domain.Entities.Identity;
using AutoGallerySaaS.Domain.Entities.SaaS;
using AutoGallerySaaS.Domain.Entities.Finance;
using AutoGallerySaaS.Domain.Entities.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.Auth.Services;

public class AuthService : IAuthService
{
    // Yeni kayitlar icin ucretsiz deneme suresi (Pro plan limitleriyle baslar).
    private const int TrialDays = 14;

    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;

    public AuthService(IApplicationDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || user.IsDeleted || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid credentials");
        }

        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == user.TenantId);

        if (tenant == null || tenant.IsDeleted || !tenant.IsActive)
        {
            throw new UnauthorizedException("Tenant is not active");
        }

        EnsureSubscriptionActive(user, tenant);

        var userRoleRows = await _context.UserRoles
            .IgnoreQueryFilters()
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => new { ur.RoleId, RoleName = ur.Role.Name })
            .ToListAsync();

        var roles = userRoleRows.Select(ur => ur.RoleName).Distinct().ToList();
        var roleIds = userRoleRows.Select(ur => ur.RoleId).Distinct().ToList();

        var permissions = await _context.RolePermissions
            .IgnoreQueryFilters()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync();

        var token = _jwtService.GenerateToken(user, roles, permissions);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        return CreateAuthResponse(user, tenant, token, refreshToken);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var emailExists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == request.Email);

        if (emailExists)
        {
            throw new BusinessRuleException("Email is already registered");
        }

        // Deneme suresi Pro plan limitleriyle baslar; Pro yoksa mevcut bir plana geriye dusulur.
        var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Name == "Pro")
            ?? await _context.SubscriptionPlans.FirstOrDefaultAsync();
        if (plan == null)
        {
            throw new Exception("Default subscription plan not found");
        }

        var tenantIdentifierBase = new string(request.TenantName
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray())
            .Trim('-');

        if (string.IsNullOrWhiteSpace(tenantIdentifierBase))
        {
            tenantIdentifierBase = "tenant";
        }

        var tenantIdentifier = tenantIdentifierBase;
        var suffix = 1;
        while (await _context.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Identifier == tenantIdentifier))
        {
            tenantIdentifier = $"{tenantIdentifierBase}-{suffix++}";
        }

        var tenant = new Tenant
        {
            Name = request.TenantName,
            Identifier = tenantIdentifier,
            SubscriptionPlanId = plan.Id,
            SubscriptionEndDate = DateTime.UtcNow.AddDays(TrialDays),
            IsTrial = true,
            IsActive = true
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var user = new User
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            TenantId = tenant.Id,
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

        var allPermissionIds = await _context.Permissions
            .IgnoreQueryFilters()
            .Select(permission => permission.Id)
            .ToListAsync();

        _context.UserRoles.Add(new UserRole
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            RoleId = adminRole.Id
        });

        _context.RolePermissions.AddRange(allPermissionIds.Select(permissionId => new RolePermission
        {
            TenantId = tenant.Id,
            RoleId = adminRole.Id,
            PermissionId = permissionId
        }));

        await _context.SaveChangesAsync();

        return await LoginAsync(new LoginRequest(request.Email, request.Password));
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

        if (user == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid refresh token");
        }

        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstAsync(t => t.Id == user.TenantId);

        EnsureSubscriptionActive(user, tenant);

        var userRoleRows = await _context.UserRoles
            .IgnoreQueryFilters()
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => new { ur.RoleId, RoleName = ur.Role.Name })
            .ToListAsync();

        var roles = userRoleRows.Select(ur => ur.RoleName).Distinct().ToList();
        var roleIds = userRoleRows.Select(ur => ur.RoleId).Distinct().ToList();

        var permissions = await _context.RolePermissions
            .IgnoreQueryFilters()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync();

        var token = _jwtService.GenerateToken(user, roles, permissions);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        return CreateAuthResponse(user, tenant, token, newRefreshToken);
    }

    /// <summary>
    /// Abonelik/deneme suresi dolan tenant'larin oturum acmasini engeller. Super admin haricidir.
    /// </summary>
    private static void EnsureSubscriptionActive(User user, Tenant tenant)
    {
        if (user.IsSuperAdmin)
        {
            return;
        }

        if (tenant.SubscriptionEndDate < DateTime.UtcNow)
        {
            var message = tenant.IsTrial
                ? "Ucretsiz deneme sureniz sona erdi. Devam etmek icin bir plan secmelisiniz."
                : "Aboneliginizin suresi doldu. Devam etmek icin aboneliginizi yenilemelisiniz.";
            throw new SubscriptionException(message);
        }
    }

    private static AuthResponse CreateAuthResponse(User user, Tenant tenant, string token, string refreshToken)
    {
        return new AuthResponse(
            token,
            refreshToken,
            DateTime.UtcNow.AddMinutes(60),
            tenant.Id,
            tenant.Name,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.IsSuperAdmin);
    }
}
