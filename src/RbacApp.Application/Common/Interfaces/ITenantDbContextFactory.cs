using Microsoft.EntityFrameworkCore;

namespace RbacApp.Application.Common.Interfaces;

/// <summary>
/// ساخت TenantDbContext با connection string متناظر با tenant جاری.
/// هر DbContext تولیدشده scoped به همان request است.
/// </summary>
public interface ITenantDbContextFactory
{
    /// <summary>
    /// یک DbContext برای tenant جاری برمی‌گرداند.
    /// اگر tenant در دسترس نباشد استثنا پرتاب می‌کند.
    /// </summary>
    ITenantDbContext CreateForCurrentTenant();

    /// <summary>
    /// یک DbContext با connection string دل‌خواه می‌سازد (برای provisioning).
    /// </summary>
    ITenantDbContext CreateForConnection(string connectionString);
}

/// <summary>
/// قرارداد دسترسی به داده‌های tenant (کاربران، نقش‌ها، دسترسی‌ها، توکن‌ها).
/// </summary>
public interface ITenantDbContext : IDisposable, IAsyncDisposable
{
    DbSet<Domain.Entities.Identity.ApplicationUser> Users { get; }
    DbSet<Domain.Entities.Identity.ApplicationRole> Roles { get; }
    DbSet<Domain.Entities.Identity.RolePermission> RolePermissions { get; }
    DbSet<Domain.Entities.Identity.RefreshToken> RefreshTokens { get; }

    /// <summary>object DbContext واقعی برای اعمال migration یا SaveChanges پیشرفته.</summary>
    DbContext Context { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
