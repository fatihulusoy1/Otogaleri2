using AutoGallerySaaS.Application.Common.Exceptions;
using AutoGallerySaaS.Application.Features.Admin.Dtos;
using AutoGallerySaaS.Application.Features.Admin.Services;
using AutoGallerySaaS.UnitTests.TestSupport;
using FluentAssertions;

namespace AutoGallerySaaS.UnitTests.Admin;

public class AdminServiceCatalogTests
{
    [Fact]
    public async Task CreateSegmentAsync_AfterDelete_ReactivatesSameRecord()
    {
        using var context = TestContext.Create(out _, out _);
        var sut = new AdminService(context);

        var created = await sut.CreateSegmentAsync(new CreateAdminSegmentRequest("Cabrio"));
        await sut.DeleteSegmentAsync(created.Id);

        // Onceki hata: silinmis kayit unique index nedeniyle yeniden eklenince cakisirdi.
        // Artik ayni kayit yeniden aktive edilmeli.
        var recreated = await sut.CreateSegmentAsync(new CreateAdminSegmentRequest("Cabrio"));

        recreated.Id.Should().Be(created.Id);
        recreated.Name.Should().Be("Cabrio");
    }

    [Fact]
    public async Task CreateSegmentAsync_WhenActiveDuplicate_ThrowsBusinessRuleException()
    {
        using var context = TestContext.Create(out _, out _);
        var sut = new AdminService(context);

        await sut.CreateSegmentAsync(new CreateAdminSegmentRequest("Cabrio"));

        var act = () => sut.CreateSegmentAsync(new CreateAdminSegmentRequest("Cabrio"));

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreateBrandAsync_AfterDelete_ReactivatesSameRecord()
    {
        using var context = TestContext.Create(out _, out _);
        var sut = new AdminService(context);

        var created = await sut.CreateBrandAsync(new CreateAdminBrandRequest("Tesla"));
        await sut.DeleteBrandAsync(created.Id);

        var recreated = await sut.CreateBrandAsync(new CreateAdminBrandRequest("Tesla"));

        recreated.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task CreateSegmentAsync_BlankName_ThrowsValidationException()
    {
        using var context = TestContext.Create(out _, out _);
        var sut = new AdminService(context);

        var act = () => sut.CreateSegmentAsync(new CreateAdminSegmentRequest("   "));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateSegmentAsync_UnknownId_ThrowsNotFoundException()
    {
        using var context = TestContext.Create(out _, out _);
        var sut = new AdminService(context);

        var act = () => sut.UpdateSegmentAsync(Guid.NewGuid(), new UpdateNameRequest("Yeni"));

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
