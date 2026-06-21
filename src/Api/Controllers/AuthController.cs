using AutoGallerySaaS.Application.Features.Auth.Dtos;
using AutoGallerySaaS.Application.Features.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoGallerySaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        return Ok(await _authService.LoginAsync(request));
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        return Ok(await _authService.RegisterAsync(request));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponse>> RefreshToken(RefreshTokenRequest request)
    {
        return Ok(await _authService.RefreshTokenAsync(request));
    }

    [HttpPost("verify-2fa")]
    public async Task<ActionResult<AuthResponse>> VerifyTwoFactor(VerifyTwoFactorRequest request)
    {
        return Ok(await _authService.VerifyTwoFactorAsync(request));
    }

    [HttpGet("email-enabled")]
    public ActionResult<object> EmailEnabled()
    {
        return Ok(new { enabled = _authService.IsEmailEnabled });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request);
        // Bilgi sızdırmamak için her durumda aynı yanıt.
        return NoContent();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);
        return NoContent();
    }

    [Authorize]
    [HttpPost("two-factor")]
    public async Task<ActionResult<ProfileDto>> SetTwoFactor(SetTwoFactorRequest request)
    {
        return Ok(await _authService.SetTwoFactorAsync(request));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ProfileDto>> GetProfile()
    {
        return Ok(await _authService.GetProfileAsync());
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<ActionResult<ProfileDto>> UpdateProfile(UpdateProfileRequest request)
    {
        return Ok(await _authService.UpdateProfileAsync(request));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        await _authService.ChangePasswordAsync(request);
        return NoContent();
    }
}
