using AutoGallerySaaS.Application.Common.Interfaces;
using AutoGallerySaaS.Application.Features.Auth.Dtos;
using AutoGallerySaaS.Application.Features.Auth.Dtos;
using AutoGallerySaaS.Application.Features.Auth.Services;
using AutoGallerySaaS.Domain.Entities.Identity;
using AutoGallerySaaS.Domain.Entities.SaaS;

using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Application.Features.Auth.Services;

public class AuthService : IAuthService
{
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
            .IgnoreQueryFilters() // Login should look across all tenants if needed, but usually limited by email
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new Exception("Invalid credentials");
        }

        var roles = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        var permissions = await _context.RolePermissions
            .Where(rp => roles.Contains(rp.Role.Name))
            .Select(rp => rp.Permission.Code)
            .ToListAsync();

        var token = _jwtService.GenerateToken(user, roles, permissions);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        return new AuthResponse(token, refreshToken, DateTime.UtcNow.AddMinutes(60));
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // 1. Create Tenant
        var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Name == "Free");
        var tenant = new Tenant
        {
            Name = request.TenantName,
            SubscriptionPlanId = plan?.Id ?? Guid.Empty,
            SubscriptionEndDate = DateTime.UtcNow.AddDays(30)
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        // 2. Create User
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

        return await LoginAsync(new LoginRequest(request.Email, request.Password));
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

        if (user == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            throw new Exception("Invalid refresh token");
        }

        var roles = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        var permissions = await _context.RolePermissions
            .Where(rp => roles.Contains(rp.Role.Name))
            .Select(rp => rp.Permission.Code)
            .ToListAsync();

        var token = _jwtService.GenerateToken(user, roles, permissions);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        return new AuthResponse(token, newRefreshToken, DateTime.UtcNow.AddMinutes(60));
    }
}
