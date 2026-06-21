using AutoGallerySaaS.Application.Features.TenantUsers.Dtos;

namespace AutoGallerySaaS.Application.Features.TenantUsers.Services;

public interface ITenantUserService
{
    Task<List<TenantUserDto>> GetUsersAsync();
    Task<TenantUserDto> CreateUserAsync(CreateTenantUserRequest request);
    Task<TenantUserDto> UpdateUserAsync(Guid userId, UpdateTenantUserRequest request);
    Task SetStatusAsync(Guid userId, SetTenantUserStatusRequest request);
    Task SetPasswordAsync(Guid userId, SetTenantUserPasswordRequest request);
    Task DeleteUserAsync(Guid userId);
}
