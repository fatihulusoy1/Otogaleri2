using AutoGallerySaaS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutoGallerySaaS.Persistence;

/// <summary>
/// EF Core arac zinciri (dotnet ef migrations / database update) icin tasarim-zamani context fabrikasi.
/// Runtime DI'dan bagimsiz calisir; yalnizca migration uretimi/uygulanmasinda kullanilir.
/// Baglanti dizesi AUTOGALLERY_DB ortam degiskeninden, yoksa yerel varsayilandan okunur.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("AUTOGALLERY_DB")
            ?? "Host=localhost;Port=5432;Database=autogallery_db;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(
            options,
            new DesignTimeTenantService(),
            new DesignTimeCurrentUserService(),
            new DesignTimeDateTime());
    }

    private sealed class DesignTimeTenantService : ITenantService
    {
        public Guid? GetTenantId() => null;
        public void SetTenantId(Guid tenantId) { }
    }

    private sealed class DesignTimeCurrentUserService : ICurrentUserService
    {
        public string? UserId => null;
        public Guid? TenantId => null;
        public bool IsAuthenticated => false;
        public bool IsSuperAdmin => false;
    }

    private sealed class DesignTimeDateTime : IDateTime
    {
        public DateTime Now => DateTime.UtcNow;
    }
}
