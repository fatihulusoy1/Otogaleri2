using AutoGallerySaaS.Application.Features.TenantUsers.Dtos;
using AutoGallerySaaS.Application.Features.TenantUsers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoGallerySaaS.Api.Controllers;

[Authorize(Roles = "TenantAdmin")]
[ApiController]
[Route("api/tenant/users")]
public class TenantUsersController : ControllerBase
{
    private readonly ITenantUserService _tenantUserService;

    public TenantUsersController(ITenantUserService tenantUserService)
    {
        _tenantUserService = tenantUserService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TenantUserDto>>> GetUsers()
    {
        return Ok(await _tenantUserService.GetUsersAsync());
    }

    [HttpPost]
    public async Task<ActionResult<TenantUserDto>> CreateUser(CreateTenantUserRequest request)
    {
        return Ok(await _tenantUserService.CreateUserAsync(request));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TenantUserDto>> UpdateUser(Guid id, UpdateTenantUserRequest request)
    {
        return Ok(await _tenantUserService.UpdateUserAsync(id, request));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> SetStatus(Guid id, SetTenantUserStatusRequest request)
    {
        await _tenantUserService.SetStatusAsync(id, request);
        return NoContent();
    }

    [HttpPost("{id}/password")]
    public async Task<IActionResult> SetPassword(Guid id, SetTenantUserPasswordRequest request)
    {
        await _tenantUserService.SetPasswordAsync(id, request);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        await _tenantUserService.DeleteUserAsync(id);
        return NoContent();
    }
}
