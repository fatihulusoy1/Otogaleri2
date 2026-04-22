using AutoGallerySaaS.Application.Features.Auth.Dtos;
namespace AutoGallerySaaS.Application.Features.Crm.Dtos;

public record CustomerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string Phone
);

public record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string? Email,
    string Phone,
    string? Address
);
