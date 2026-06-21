using System.IdentityModel.Tokens.Jwt;
using AutoGallerySaaS.Application.Features.Auth.Services;
using AutoGallerySaaS.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using AutoGallerySaaS.Infrastructure.Authentication;

namespace AutoGallerySaaS.UnitTests.Auth;

public class JwtServiceTests
{
    private readonly JwtService _sut;
    private readonly Mock<IConfiguration> _configMock;

    public JwtServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(x => x["Jwt:Key"]).Returns("super_secret_key_1234567890123456");
        _configMock.Setup(x => x["Jwt:Issuer"]).Returns("AutoGallerySaaS");
        _configMock.Setup(x => x["Jwt:Audience"]).Returns("AutoGallerySaaSUsers");
        _configMock.Setup(x => x["Jwt:ExpireMinutes"]).Returns("60");

        _sut = new JwtService(_configMock.Object);
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidTokenString()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com", TenantId = Guid.NewGuid() };
        var roles = new List<string> { "Admin" };
        var permissions = new List<string> { "Vehicles.View" };

        // Act
        var result = _sut.GenerateToken(user, roles, permissions);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_ShouldEmbedTenantSuperAdminRoleAndPermissionClaims()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.com",
            TenantId = tenantId,
            IsSuperAdmin = true
        };
        var roles = new List<string> { "TenantAdmin" };
        var permissions = new List<string> { "Vehicles.View", "Finance.Manage" };

        // Act
        var token = _sut.GenerateToken(user, roles, permissions);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert
        jwt.Claims.Should().Contain(c => c.Type == "tenant_id" && c.Value == tenantId.ToString());
        // is_super_admin claim'i policy ile uyumlu olmali (RequireClaim bool.TrueString => "True").
        jwt.Claims.Should().Contain(c => c.Type == "is_super_admin" && c.Value == bool.TrueString);
        jwt.Claims.Should().Contain(c => c.Type == "permission" && c.Value == "Vehicles.View");
        jwt.Claims.Should().Contain(c => c.Type == "permission" && c.Value == "Finance.Manage");
    }
}
