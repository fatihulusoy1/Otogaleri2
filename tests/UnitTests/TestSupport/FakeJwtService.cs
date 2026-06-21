using AutoGallerySaaS.Application.Features.Auth.Services;
using AutoGallerySaaS.Domain.Entities.Identity;

namespace AutoGallerySaaS.UnitTests.TestSupport;

/// <summary>
/// Testlerde gercek imzalama yapmadan sabit token ureten <see cref="IJwtService"/> sahtesi.
/// </summary>
public sealed class FakeJwtService : IJwtService
{
    public string GenerateToken(User user, List<string> roles, List<string> permissions) => "test-token";

    public string GenerateRefreshToken() => "test-refresh-token";
}
