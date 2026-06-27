using RbacApp.Application.Common.Interfaces;

namespace RbacApp.Application.Common.Interfaces;

/// <summary>
/// اعمال seed اولیه روی یک دیتابیس tenant تازه‌ساخته‌شده:
/// نقش "Admin" با تمام دسترسی‌ها + ایجاد اولین ادمین.
/// </summary>
public interface ITenantSeeder
{
    /// <summary>
    /// نقش‌های پیش‌فرض سیستم را seed می‌کند (Admin با تمام دسترسی‌ها).
    /// باید قبل از ایجاد اولین کاربر فراخوانی شود.
    /// </summary>
    Task SeedRolesAsync(ITenantDbContext db, CancellationToken ct = default);

    /// <summary>
    /// اولین ادمین tenant را ایجاد می‌کند و نقش Admin را به او می‌دهد.
    /// </summary>
    Task SeedFirstAdminAsync(
        ITenantDbContext db,
        string fullName,
        string email,
        string password,
        CancellationToken ct = default);
}
