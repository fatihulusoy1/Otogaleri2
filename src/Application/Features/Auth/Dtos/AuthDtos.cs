using AutoGallerySaaS.Application.Features.Auth.Dtos;
namespace AutoGallerySaaS.Application.Features.Auth.Dtos;

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string Password, string FirstName, string LastName, string TenantName);
public record RefreshTokenRequest(string Token, string RefreshToken);
public record AuthResponse(string Token, string RefreshToken, DateTime Expiry);
