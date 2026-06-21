using AutoGallerySaaS.Application.Features.Admin.Dtos;
using AutoGallerySaaS.Application.Features.Finance.Dtos;
using AutoGallerySaaS.Application.Features.TenantActivities.Dtos;
using AutoGallerySaaS.Domain.Entities.Finance;

namespace AutoGallerySaaS.Application.Features.Admin.Services;

public interface IAdminService
{
    Task<List<AdminTenantDto>> GetTenantsAsync();
    Task<List<AdminSubscriptionPlanDto>> GetSubscriptionPlansAsync();
    Task<AdminSubscriptionPlanDto> CreatePlanAsync(CreatePlanRequest request);
    Task<AdminSubscriptionPlanDto> UpdatePlanAsync(Guid planId, UpdatePlanRequest request);
    Task DeletePlanAsync(Guid planId);
    Task<List<AdminUserDto>> GetUsersAsync(Guid? tenantId = null);
    Task UpdateTenantStatusAsync(Guid tenantId, bool isActive);
    Task UpdateUserStatusAsync(Guid userId, bool isActive);
    Task<AdminTenantDto> CreateTenantAsync(CreateTenantRequest request);
    Task<AdminTenantDto> UpdateTenantAsync(Guid tenantId, UpdateTenantRequest request);
    Task DeleteTenantAsync(Guid tenantId);
    Task<List<TenantActivityDto>> GetTenantActivitiesAsync(Guid tenantId);
    Task<AdminUserDto> CreateUserAsync(CreateUserRequest request);
    Task<AdminUserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task DeleteUserAsync(Guid userId);
    Task SetUserPasswordAsync(Guid userId, SetUserPasswordRequest request);
    Task<AdminCatalogLookupsDto> GetCatalogAsync();
    Task<AdminLookupDto> CreateSegmentAsync(CreateAdminSegmentRequest request);
    Task<AdminLookupDto> UpdateSegmentAsync(Guid id, UpdateNameRequest request);
    Task DeleteSegmentAsync(Guid id);
    Task<AdminLookupDto> CreateBrandAsync(CreateAdminBrandRequest request);
    Task<AdminLookupDto> UpdateBrandAsync(Guid id, UpdateNameRequest request);
    Task DeleteBrandAsync(Guid id);
    Task<AdminCatalogModelDto> CreateModelAsync(CreateAdminModelRequest request);
    Task<AdminCatalogModelDto> UpdateModelAsync(Guid id, CreateAdminModelRequest request);
    Task DeleteModelAsync(Guid id);
    Task<List<ExpenseCategoryDto>> GetExpenseCategoriesAsync(ExpenseCategoryType? categoryType = null);
    Task<ExpenseCategoryDto> CreateExpenseCategoryAsync(CreateExpenseCategoryRequest request);
    Task<ExpenseCategoryDto> UpdateExpenseCategoryAsync(Guid id, UpdateNameRequest request);
    Task DeleteExpenseCategoryAsync(Guid id);
}
