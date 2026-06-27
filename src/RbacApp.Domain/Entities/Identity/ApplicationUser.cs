using Microsoft.AspNetCore.Identity;
using RbacApp.Domain.Common;

namespace RbacApp.Domain.Entities.Identity;

/// <summary>
/// کاربر در محدوده‌ی یک tenant (در دیتابیس tenant ذخیره می‌شود).
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>نام نمایشی.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>نام فارسی/محلی (اختیاری).</summary>
    public string? DisplayName { get; set; }

    /// <summary>تاریخ آخرین ورود.</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>غیرفعال شده توسط ادمین (مستقل از قفل Identity).</summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}
