using AutoGallerySaaS.Application.Features.Auth.Dtos;
using AutoGallerySaaS.Domain.Entities.Identity;

namespace AutoGallerySaaS.Application.Features.Auth.Services;

public interface IJwtService
{
    string GenerateToken(User user, List<string> roles, List<string> permissions);
    string GenerateRefreshToken();
}

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
}
