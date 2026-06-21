using AutoGallerySaaS.Application.Features.Admin.Dtos;
using AutoGallerySaaS.Application.Features.Admin.Services;
using AutoGallerySaaS.Application.Features.Finance.Dtos;
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

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDto>>> GetUsers([FromQuery] Guid? tenantId)
    {
        return Ok(await _adminService.GetUsersAsync(tenantId));
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
