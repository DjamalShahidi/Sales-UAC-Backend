using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Domain.Enums;

namespace RbacApp.Infrastructure.MultiTenancy;

/// <summary>
/// tenant جاری را از درخواست resolve کرده و در ITenantContext (scoped) قرار می‌دهد.
/// منابع resolve (به ترتیب اولویت):
///   1) header "X-Tenant-Slug"
///   2) اولین بخش از subdomain (مثلا "acme" از acme.example.com)
/// مسیرهای /api/admin/* و /api/auth بدون نیاز به tenant کار می‌کنند.
/// </summary>
public class TenantResolverMiddleware
{
    private const string TenantSlugHeader = "X-Tenant-Slug";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolverMiddleware> _logger;

    public TenantResolverMiddleware(RequestDelegate next, ILogger<TenantResolverMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICatalogDbContext catalog,
        ITenantContext tenantContext,
        IMemoryCache cache)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // مسیرهای super-admin نیاز به tenant ندارند.
        var isAdminScope = path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase);

        // مسیرهای عمومی که tenant لازم ندارند (مثل swagger).
        if (IsPublicPath(path))
        {
            await _next(context);
            return;
        }

        if (isAdminScope)
        {
            tenantContext.SetSuperAdminScope();
            await _next(context);
            return;
        }

        var slug = ResolveSlug(context.Request);
        if (string.IsNullOrWhiteSpace(slug))
        {
            // برای /api/auth/refresh و /api/auth/logout، tenant از توکن گرفته می‌شود (در handler).
            // در غیر این صورت بدون tenant ادامه می‌دهیم تا endpoint نحوه‌ی پاسخ را تعیین کند.
            await _next(context);
            return;
        }

        try
        {
            var tenant = await ResolveTenantAsync(slug, catalog, cache);
            if (tenant is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    errorCode = "tenant_not_found",
                    message = $"tenant با slug '{slug}' پیدا نشد."
                });
                return;
            }

            if (tenant.Status == TenantStatus.Suspended)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    errorCode = "tenant_suspended",
                    message = "این سازمان معلق شده است."
                });
                return;
            }

            tenantContext.SetTenant(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در resolve tenant برای slug {Slug}", slug);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                errorCode = "tenant_resolution_failed",
                message = "خطا در شناسایی سازمان."
            });
            return;
        }

        await _next(context);
    }

    private static bool IsPublicPath(string path)
        => path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
           || path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase)
           || path == "/health";

    private static string? ResolveSlug(HttpRequest request)
    {
        // 1) header صریح.
        if (request.Headers.TryGetValue(TenantSlugHeader, out var headerSlug)
            && !string.IsNullOrWhiteSpace(headerSlug))
            return headerSlug.ToString().Trim().ToLowerInvariant();

        // 2) subdomain (اولین برش قبل از اولین نقطه، در صورت وجود دامنه).
        var host = request.Host.Host;
        if (!HttpContextLocal.IsLocalhost(host))
        {
            var parts = host.Split('.');
            if (parts.Length >= 3 && parts[0] != "www")
                return parts[0].ToLowerInvariant();
        }

        return null;
    }

    private static async Task<Domain.Entities.Tenant?> ResolveTenantAsync(
        string slug, ICatalogDbContext catalog, IMemoryCache cache)
    {
        var cacheKey = $"tenant:{slug}";
        if (cache.TryGetValue(cacheKey, out Domain.Entities.Tenant? cached) && cached is not null)
            return cached;

        var tenant = await catalog.FindBySlugAsync(slug);
        if (tenant is not null)
            cache.Set(cacheKey, tenant, CacheTtl);

        return tenant;
    }
}

internal static class HttpContextLocal
{
    public static bool IsLocalhost(string host)
        => host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
           || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
}
