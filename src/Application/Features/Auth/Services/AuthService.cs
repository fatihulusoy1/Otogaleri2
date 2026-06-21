using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Auth.Dtos;
using AutoGallerySaaS.Domain.Entities.Identity;
using AutoGallerySaaS.Domain.Entities.SaaS;
using AutoGallerySaaS.Domain.Entities.Finance;
using AutoGallerySaaS.Domain.Entities.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AutoGallerySaaS.Application.Features.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILoginThrottle _loginThrottle;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly AutoGallerySaaS.Application.Features.TenantActivities.Services.ITenantActivityService _activityLog;

    public AuthService(
        IApplicationDbContext context,
        IJwtService jwtService,
        ICurrentUserService currentUser,
        ILoginThrottle loginThrottle,
        IEmailService emailService,
        IConfiguration configuration,
        AutoGallerySaaS.Application.Features.TenantActivities.Services.ITenantActivityService activityLog)
    {
        _context = context;
        _jwtService = jwtService;
        _currentUser = currentUser;
        _loginThrottle = loginThrottle;
        _emailService = emailService;
        _configuration = configuration;
        _activityLog = activityLog;
    }

    public bool IsEmailEnabled => _emailService.IsConfigured;

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // E-posta tenant başına benzersiz olduğundan aynı e-posta farklı tenant'larda olabilir.
        // Bu yüzden e-posta + şifreyle eşleşen aktif kullanıcıyı tenant'lar arasında çözeriz.
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

        // Brute-force koruması: çok fazla başarısız denemede geçici kilit.
        _loginThrottle.EnsureNotLocked(email);

        var candidates = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Email.ToLower() == email && !u.IsDeleted && u.IsActive)
            .ToListAsync();

        var matchingUsers = candidates
            .Where(u => BCrypt.Net.BCrypt.Verify(request.Password, u.PasswordHash))
            .ToList();

        if (matchingUsers.Count == 0)
        {
            _loginThrottle.RegisterFailure(email);
            foreach (var failedTenantId in candidates.Select(c => c.TenantId).Distinct())
            {
                await _activityLog.LogAsync(failedTenantId, TenantActivityType.UserLoginFailed, $"Başarısız giriş denemesi: {email}");
            }
            throw new Exception("Invalid credentials");
        }

        User? user = null;
        Tenant? tenant = null;
        foreach (var candidate in matchingUsers)
        {
            var candidateTenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == candidate.TenantId);

            if (candidateTenant == null || candidateTenant.IsDeleted)
            {
                continue;
            }

            // Super admin (merkezi destek ekibi) tenant pasif olsa bile giriş yapabilir.
            if (!candidate.IsSuperAdmin && !candidateTenant.IsActive)
            {
                continue;
            }

            user = candidate;
            tenant = candidateTenant;
            break;
        }

        if (user == null || tenant == null)
        {
            throw new Exception("Tenant is not active");
        }

        // Başarılı şifre doğrulaması: başarısız deneme sayacını sıfırla.
        _loginThrottle.Reset(email);

        // İki adımlı doğrulama açıksa: kod üret, e-posta ile gönder ve token verme.
        if (user.TwoFactorEnabled)
        {
            await IssueTwoFactorCodeAsync(user);
            return TwoFactorRequiredResponse();
        }

        return await CompleteLoginAsync(user, tenant);
    }

    public async Task<AuthResponse> VerifyTwoFactorAsync(VerifyTwoFactorRequest request)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        _loginThrottle.EnsureNotLocked(email);

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && !u.IsDeleted && u.IsActive && u.TwoFactorEnabled);

        var code = (request.Code ?? string.Empty).Trim();
        if (user == null ||
            string.IsNullOrEmpty(user.TwoFactorCodeHash) ||
            user.TwoFactorCodeExpiresAt == null ||
            user.TwoFactorCodeExpiresAt < DateTime.UtcNow ||
            !BCrypt.Net.BCrypt.Verify(code, user.TwoFactorCodeHash))
        {
            _loginThrottle.RegisterFailure(email);
            throw new Exception("Doğrulama kodu hatalı veya süresi dolmuş.");
        }

        var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == user.TenantId);
        if (tenant == null || tenant.IsDeleted || (!user.IsSuperAdmin && !tenant.IsActive))
        {
            throw new Exception("Tenant is not active");
        }

        _loginThrottle.Reset(email);

        // Kodu tüket.
        user.TwoFactorCodeHash = null;
        user.TwoFactorCodeExpiresAt = null;

        return await CompleteLoginAsync(user, tenant);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        // Mail yapılandırılmadıysa özellik pasiftir (sessizce çıkar; bilgi sızdırmaz).
        if (!_emailService.IsConfigured)
        {
            return;
        }

        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var users = await _context.Users.IgnoreQueryFilters()
            .Where(u => u.Email.ToLower() == email && !u.IsDeleted && u.IsActive)
            .ToListAsync();

        var clientUrl = (_configuration["App:ClientUrl"] ?? "http://localhost:5173").TrimEnd('/');

        foreach (var user in users)
        {
            var token = GenerateUrlToken();
            user.PasswordResetTokenHash = Sha256Hex(token);
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            var link = $"{clientUrl}/reset-password?token={token}";
            await _emailService.SendAsync(
                user.Email,
                "Şifre sıfırlama",
                $"<p>Merhaba {user.FirstName},</p><p>Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayın (1 saat geçerli):</p><p><a href=\"{link}\">{link}</a></p><p>Bu isteği siz yapmadıysanız bu e-postayı yok sayabilirsiniz.</p>");
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            throw new Exception("Yeni şifre en az 6 karakter olmalıdır.");
        }

        var tokenHash = Sha256Hex(request.Token ?? string.Empty);
        var user = await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PasswordResetTokenHash == tokenHash && !u.IsDeleted);

        if (user == null || user.PasswordResetTokenExpiresAt == null || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
        {
            throw new Exception("Bağlantı geçersiz veya süresi dolmuş.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAt = null;
        // Sıfırlamada açık oturumları geçersiz kıl.
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(user.TenantId, TenantActivityType.PasswordReset, $"Şifre sıfırlandı (e-posta ile): {user.Email}");
    }

    private static string GenerateUrlToken()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", string.Empty);
    }

    private static string Sha256Hex(string input)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
    }

    private async Task IssueTwoFactorCodeAsync(User user)
    {
        var code = GenerateNumericCode();
        user.TwoFactorCodeHash = BCrypt.Net.BCrypt.HashPassword(code);
        user.TwoFactorCodeExpiresAt = DateTime.UtcNow.AddMinutes(5);
        await _context.SaveChangesAsync();

        await _emailService.SendAsync(
            user.Email,
            "Giriş doğrulama kodunuz",
            $"<p>Merhaba {user.FirstName},</p><p>Giriş doğrulama kodunuz: <strong style=\"font-size:20px\">{code}</strong></p><p>Bu kod 5 dakika geçerlidir.</p>");
    }

    private static string GenerateNumericCode()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(4);
        var value = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
        return value.ToString("D6");
    }

    private async Task<AuthResponse> CompleteLoginAsync(User user, Tenant tenant)
    {
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
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(tenant.Id, TenantActivityType.UserLoggedIn, $"Giriş yapıldı: {user.Email}");

        return CreateAuthResponse(user, tenant, token, refreshToken, roles.Contains("TenantAdmin"));
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // E-posta tenant başına benzersizdir; kayıt yeni bir tenant oluşturduğu için
        // aynı e-posta başka tenant'larda mevcut olsa bile yeni kayıt engellenmez.
        var registerEmail = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

        var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == request.SubscriptionPlanId && p.IsActive);
        if (plan == null)
        {
            plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Name == "Free");
        }
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

        var subscriptionEndDate = request.BillingCycle == BillingCycle.Yearly
            ? DateTime.UtcNow.AddYears(1)
            : DateTime.UtcNow.AddMonths(1);

        var tenant = new Tenant
        {
            Name = request.TenantName,
            Identifier = tenantIdentifier,
            SubscriptionPlanId = plan.Id,
            SubscriptionEndDate = subscriptionEndDate,
            EffectiveMaxUsers = plan.MaxUsers,
            EffectiveMaxVehicles = plan.MaxVehicles,
            IsActive = true
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var user = new User
        {
            Email = registerEmail,
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

        var amount = request.BillingCycle == BillingCycle.Yearly ? plan.YearlyPrice : plan.MonthlyPrice;
        var cycleLabel = request.BillingCycle == BillingCycle.Yearly ? "Yıllık" : "Aylık";
        await _activityLog.LogAsync(
            tenant.Id,
            TenantActivityType.SubscriptionPurchased,
            $"{plan.Name} paketi · {cycleLabel} · bitiş: {tenant.SubscriptionEndDate:yyyy-MM-dd}",
            amount);

        return await LoginAsync(new LoginRequest(request.Email, request.Password));
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

        // Pasif/silinmiş kullanıcı refresh token ile yeni oturum açamaz.
        if (user == null || user.IsDeleted || !user.IsActive || user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            throw new Exception("Invalid refresh token");
        }

        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == user.TenantId);

        if (tenant == null || tenant.IsDeleted || (!user.IsSuperAdmin && !tenant.IsActive))
        {
            throw new Exception("Tenant is not active");
        }

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

        return CreateAuthResponse(user, tenant, token, newRefreshToken, roles.Contains("TenantAdmin"));
    }

    public async Task<ProfileDto> GetProfileAsync()
    {
        var (user, tenant, roleLabel) = await LoadCurrentUserAsync();
        return BuildProfile(user, tenant, roleLabel);
    }

    public async Task<ProfileDto> UpdateProfileAsync(UpdateProfileRequest request)
    {
        var (user, tenant, roleLabel) = await LoadCurrentUserAsync();

        var firstName = (request.FirstName ?? string.Empty).Trim();
        var lastName = (request.LastName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new Exception("Ad zorunludur.");
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(tenant.Id, TenantActivityType.ProfileUpdated, $"Profil güncellendi: {user.Email}");

        return BuildProfile(user, tenant, roleLabel);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request)
    {
        var (user, tenant, _) = await LoadCurrentUserAsync();

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            throw new Exception("Yeni şifre en az 6 karakter olmalıdır.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new Exception("Mevcut şifre hatalı.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(tenant.Id, TenantActivityType.PasswordChanged, $"Şifre değiştirildi: {user.Email}");
    }

    public async Task<ProfileDto> SetTwoFactorAsync(SetTwoFactorRequest request)
    {
        var (user, tenant, roleLabel) = await LoadCurrentUserAsync();

        user.TwoFactorEnabled = request.Enabled;
        if (!request.Enabled)
        {
            user.TwoFactorCodeHash = null;
            user.TwoFactorCodeExpiresAt = null;
        }
        await _context.SaveChangesAsync();

        return BuildProfile(user, tenant, roleLabel);
    }

    private async Task<(User User, Tenant Tenant, string RoleLabel)> LoadCurrentUserAsync()
    {
        if (!Guid.TryParse(_currentUser.UserId, out var userId))
        {
            throw new Exception("Oturum bulunamadı.");
        }

        var user = await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == userId && !item.IsDeleted);
        if (user == null)
        {
            throw new Exception("Kullanıcı bulunamadı.");
        }

        var tenant = await _context.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == user.TenantId);
        if (tenant == null)
        {
            throw new Exception("Tenant bulunamadı.");
        }

        var roleNames = await _context.UserRoles.IgnoreQueryFilters()
            .Where(item => item.UserId == user.Id)
            .Select(item => item.Role.Name)
            .ToListAsync();

        var roleLabel = user.IsSuperAdmin
            ? "Super Admin"
            : roleNames.Contains("TenantAdmin")
                ? "Tenant Yöneticisi"
                : "Kullanıcı";

        return (user, tenant, roleLabel);
    }

    private static ProfileDto BuildProfile(User user, Tenant tenant, string roleLabel)
    {
        var subscriptionExpired = !user.IsSuperAdmin && tenant.SubscriptionEndDate < DateTime.UtcNow;

        return new ProfileDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            tenant.Id,
            tenant.Name,
            user.IsSuperAdmin,
            roleLabel,
            user.CreatedAt,
            tenant.SubscriptionEndDate,
            subscriptionExpired,
            user.LastLoginAt,
            user.TwoFactorEnabled);
    }

    private static AuthResponse CreateAuthResponse(User user, Tenant tenant, string token, string refreshToken, bool isTenantAdmin)
    {
        var subscriptionExpired = !user.IsSuperAdmin && tenant.SubscriptionEndDate < DateTime.UtcNow;

        return new AuthResponse(
            token,
            refreshToken,
            DateTime.UtcNow.AddMinutes(60),
            tenant.Id,
            tenant.Name,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.IsSuperAdmin,
            tenant.SubscriptionEndDate,
            subscriptionExpired,
            isTenantAdmin,
            false);
    }

    private static AuthResponse TwoFactorRequiredResponse()
    {
        return new AuthResponse(
            string.Empty,
            string.Empty,
            DateTime.UtcNow,
            Guid.Empty,
            string.Empty,
            string.Empty,
            false,
            DateTime.UtcNow,
            false,
            false,
            true);
    }
}
