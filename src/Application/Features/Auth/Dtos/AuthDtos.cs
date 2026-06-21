using AutoGallerySaaS.Domain.Entities.SaaS;

namespace AutoGallerySaaS.Application.Features.Auth.Dtos;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string TenantName,
    Guid SubscriptionPlanId,
    BillingCycle BillingCycle);

public record RefreshTokenRequest(string Token, string RefreshToken);

public record VerifyTwoFactorRequest(string Email, string Code);

public record SetTwoFactorRequest(bool Enabled);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Token, string NewPassword);

public record ProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    Guid TenantId,
    string TenantName,
    bool IsSuperAdmin,
    string RoleLabel,
    DateTime CreatedAt,
    DateTime SubscriptionEndDate,
    bool SubscriptionExpired,
    DateTime? LastLoginAt,
    bool TwoFactorEnabled);

public record UpdateProfileRequest(
    string FirstName,
    string LastName);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public record AuthResponse(
    string Token,
    string RefreshToken,
    DateTime Expiry,
    Guid TenantId,
    string TenantName,
    string UserFullName,
    bool IsSuperAdmin,
    DateTime SubscriptionEndDate,
    bool SubscriptionExpired,
    bool IsTenantAdmin,
    bool RequiresTwoFactor);
