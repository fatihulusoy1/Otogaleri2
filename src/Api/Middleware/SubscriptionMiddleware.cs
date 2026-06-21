using System.Security.Claims;
using AutoGallerySaaS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoGallerySaaS.Api.Middleware;

/// <summary>
/// Her kimliği doğrulanmış istekte oturum geçerliliğini ve abonelik durumunu denetler:
/// 1) Kullanıcı silinmiş/pasif ise (admin tarafından çıkarılmış) anında 401 — eldeki access token süresi
///    dolmamış olsa bile o andan itibaren hiçbir veriye erişemez.
/// 2) Tenant silinmiş/pasif ise 401 (super admin tenant pasif olsa da girebilir).
/// 3) Tenant'ın aboneliği dolmuşsa (super admin hariç), abonelik/auth dışındaki uç noktalar 403 döner;
///    istemci kullanıcıyı abonelik ekranına yönlendirir.
/// </summary>
public class SubscriptionMiddleware
{
    private readonly RequestDelegate _next;

    public SubscriptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IApplicationDbContext dbContext)
    {
        var principal = context.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            var isSuperAdmin = string.Equals(
                principal.FindFirstValue("is_super_admin"),
                bool.TrueString,
                StringComparison.OrdinalIgnoreCase);

            Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
            Guid.TryParse(principal.FindFirstValue("tenant_id"), out var tenantId);

            // 1) Hesap hâlâ geçerli mi? (pasif/silinmiş kullanıcı anında engellenir)
            var account = await dbContext.Users
                .IgnoreQueryFilters()
                .Where(user => user.Id == userId)
                .Select(user => new { user.IsDeleted, user.IsActive })
                .FirstOrDefaultAsync();

            if (account == null || account.IsDeleted || !account.IsActive)
            {
                await WriteForbiddenAsync(context, StatusCodes.Status401Unauthorized, "session_revoked", "Oturumunuz geçersiz. Lütfen tekrar giriş yapın.");
                return;
            }

            // 2) Tenant geçerli mi?
            var tenant = await dbContext.Tenants
                .IgnoreQueryFilters()
                .Where(item => item.Id == tenantId)
                .Select(item => new { item.IsDeleted, item.IsActive, item.SubscriptionEndDate })
                .FirstOrDefaultAsync();

            if (tenant == null || tenant.IsDeleted || (!isSuperAdmin && !tenant.IsActive))
            {
                await WriteForbiddenAsync(context, StatusCodes.Status401Unauthorized, "session_revoked", "Hesabınıza erişim kapatıldı.");
                return;
            }

            // 3) Abonelik süresi denetimi (super admin muaf; abonelik/auth yolları serbest)
            if (!isSuperAdmin && tenant.SubscriptionEndDate < DateTime.UtcNow)
            {
                var path = context.Request.Path.Value ?? string.Empty;
                var isAllowedPath =
                    path.StartsWith("/api/subscription", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase);

                if (!isAllowedPath)
                {
                    await WriteForbiddenAsync(context, StatusCodes.Status403Forbidden, "subscription_expired", "Aboneliğinizin süresi doldu. Lütfen aboneliğinizi yenileyin.");
                    return;
                }
            }
        }

        await _next(context);
    }

    private static async Task WriteForbiddenAsync(HttpContext context, int statusCode, string code, string title)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync($"{{\"code\":\"{code}\",\"title\":\"{title}\"}}");
    }
}
