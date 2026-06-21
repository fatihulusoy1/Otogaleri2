using AutoGallerySaaS.Application.Features.Admin.Dtos;
using AutoGallerySaaS.Application.Features.Admin.Services;
using AutoGallerySaaS.Application.Features.Finance.Dtos;
using AutoGallerySaaS.Application.Features.TenantActivities.Dtos;
using AutoGallerySaaS.Domain.Entities.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoGallerySaaS.Api.Controllers;

[Authorize(Policy = "SuperAdminOnly")]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("tenants")]
    public async Task<ActionResult<List<AdminTenantDto>>> GetTenants()
    {
        return Ok(await _adminService.GetTenantsAsync());
    }

    [HttpGet("subscription-plans")]
    public async Task<ActionResult<List<AdminSubscriptionPlanDto>>> GetSubscriptionPlans()
    {
        return Ok(await _adminService.GetSubscriptionPlansAsync());
    }

    [HttpPost("subscription-plans")]
    public async Task<ActionResult<AdminSubscriptionPlanDto>> CreatePlan(CreatePlanRequest request)
    {
        return Ok(await _adminService.CreatePlanAsync(request));
    }

    [HttpPut("subscription-plans/{id}")]
    public async Task<ActionResult<AdminSubscriptionPlanDto>> UpdatePlan(Guid id, UpdatePlanRequest request)
    {
        return Ok(await _adminService.UpdatePlanAsync(id, request));
    }

    [HttpDelete("subscription-plans/{id}")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        await _adminService.DeletePlanAsync(id);
        return NoContent();
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDto>>> GetUsers([FromQuery] Guid? tenantId)
    {
        return Ok(await _adminService.GetUsersAsync(tenantId));
    }

    [HttpPost("tenants")]
    public async Task<ActionResult<AdminTenantDto>> CreateTenant(CreateTenantRequest request)
    {
        return Ok(await _adminService.CreateTenantAsync(request));
    }

    [HttpPut("tenants/{id}")]
    public async Task<ActionResult<AdminTenantDto>> UpdateTenant(Guid id, UpdateTenantRequest request)
    {
        return Ok(await _adminService.UpdateTenantAsync(id, request));
    }

    [HttpDelete("tenants/{id}")]
    public async Task<IActionResult> DeleteTenant(Guid id)
    {
        await _adminService.DeleteTenantAsync(id);
        return NoContent();
    }

    [HttpGet("tenants/{id}/activities")]
    public async Task<ActionResult<List<TenantActivityDto>>> GetTenantActivities(Guid id)
    {
        return Ok(await _adminService.GetTenantActivitiesAsync(id));
    }

    [HttpPost("users")]
    public async Task<ActionResult<AdminUserDto>> CreateUser(CreateUserRequest request)
    {
        return Ok(await _adminService.CreateUserAsync(request));
    }

    [HttpPut("users/{id}")]
    public async Task<ActionResult<AdminUserDto>> UpdateUser(Guid id, UpdateUserRequest request)
    {
        return Ok(await _adminService.UpdateUserAsync(id, request));
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        await _adminService.DeleteUserAsync(id);
        return NoContent();
    }

    [HttpPost("users/{id}/password")]
    public async Task<IActionResult> SetUserPassword(Guid id, SetUserPasswordRequest request)
    {
        await _adminService.SetUserPasswordAsync(id, request);
        return NoContent();
    }

    [HttpPatch("tenants/{id}/status")]
    public async Task<IActionResult> UpdateTenantStatus(Guid id, UpdateStatusRequest request)
    {
        await _adminService.UpdateTenantStatusAsync(id, request.IsActive);
        return NoContent();
    }

    [HttpPatch("users/{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, UpdateStatusRequest request)
    {
        await _adminService.UpdateUserStatusAsync(id, request.IsActive);
        return NoContent();
    }

    [HttpGet("catalog")]
    public async Task<ActionResult<AdminCatalogLookupsDto>> GetCatalog()
    {
        return Ok(await _adminService.GetCatalogAsync());
    }

    [HttpPost("catalog/segments")]
    public async Task<ActionResult<AdminLookupDto>> CreateSegment(CreateAdminSegmentRequest request)
    {
        return Ok(await _adminService.CreateSegmentAsync(request));
    }

    [HttpPut("catalog/segments/{id}")]
    public async Task<ActionResult<AdminLookupDto>> UpdateSegment(Guid id, UpdateNameRequest request)
    {
        return Ok(await _adminService.UpdateSegmentAsync(id, request));
    }

    [HttpDelete("catalog/segments/{id}")]
    public async Task<IActionResult> DeleteSegment(Guid id)
    {
        await _adminService.DeleteSegmentAsync(id);
        return NoContent();
    }

    [HttpPost("catalog/brands")]
    public async Task<ActionResult<AdminLookupDto>> CreateBrand(CreateAdminBrandRequest request)
    {
        return Ok(await _adminService.CreateBrandAsync(request));
    }

    [HttpPut("catalog/brands/{id}")]
    public async Task<ActionResult<AdminLookupDto>> UpdateBrand(Guid id, UpdateNameRequest request)
    {
        return Ok(await _adminService.UpdateBrandAsync(id, request));
    }

    [HttpDelete("catalog/brands/{id}")]
    public async Task<IActionResult> DeleteBrand(Guid id)
    {
        await _adminService.DeleteBrandAsync(id);
        return NoContent();
    }

    [HttpPost("catalog/models")]
    public async Task<ActionResult<AdminCatalogModelDto>> CreateModel(CreateAdminModelRequest request)
    {
        return Ok(await _adminService.CreateModelAsync(request));
    }

    [HttpPut("catalog/models/{id}")]
    public async Task<ActionResult<AdminCatalogModelDto>> UpdateModel(Guid id, CreateAdminModelRequest request)
    {
        return Ok(await _adminService.UpdateModelAsync(id, request));
    }

    [HttpDelete("catalog/models/{id}")]
    public async Task<IActionResult> DeleteModel(Guid id)
    {
        await _adminService.DeleteModelAsync(id);
        return NoContent();
    }

    [HttpGet("expense-categories")]
    public async Task<ActionResult<List<ExpenseCategoryDto>>> GetExpenseCategories([FromQuery] ExpenseCategoryType? categoryType)
    {
        return Ok(await _adminService.GetExpenseCategoriesAsync(categoryType));
    }

    [HttpPost("expense-categories")]
    public async Task<ActionResult<ExpenseCategoryDto>> CreateExpenseCategory(CreateExpenseCategoryRequest request)
    {
        return Ok(await _adminService.CreateExpenseCategoryAsync(request));
    }

    [HttpPut("expense-categories/{id}")]
    public async Task<ActionResult<ExpenseCategoryDto>> UpdateExpenseCategory(Guid id, UpdateNameRequest request)
    {
        return Ok(await _adminService.UpdateExpenseCategoryAsync(id, request));
    }

    [HttpDelete("expense-categories/{id}")]
    public async Task<IActionResult> DeleteExpenseCategory(Guid id)
    {
        await _adminService.DeleteExpenseCategoryAsync(id);
        return NoContent();
    }
}
