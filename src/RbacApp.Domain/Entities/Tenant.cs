using RbacApp.Domain.Common;
using RbacApp.Domain.Enums;

namespace RbacApp.Domain.Entities;

/// <summary>
/// رکورد یک tenant در دیتابیس مرکزی (Catalog).
/// <see cref="ConnectionName"/> کلید نام connection string در appsettings است،
/// نه خود رشته‌ی اتصال — برای امنیت.
/// </summary>
public class Tenant : BaseEntity
{
    /// <summary>
    /// نام نمایشی سازمان/tenant.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// شناسه‌ی یکتای قابل استفاده در URL/subdomain (مثلا "acme").
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// نام مستعار connection string در پیکربندی (نه خود رشته‌ی اتصال).
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// ایمیل ادمین اولیه‌ی tenant (هنگام provisioning ثبت می‌شود).
    /// </summary>
    public string? AdminEmail { get; set; }

    public TenantStatus Status { get; set; } = TenantStatus.Active;

    public DateTime? LastAccessedAt { get; set; }
}
