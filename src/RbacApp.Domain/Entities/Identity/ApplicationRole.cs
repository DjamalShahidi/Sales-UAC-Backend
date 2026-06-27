using Microsoft.AspNetCore.Identity;

namespace RbacApp.Domain.Entities.Identity;

/// <summary>
/// نقش در محدوده‌ی یک tenant.
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    /// <summary>توضیح نقش برای ادمین.</summary>
    public string? Description { get; set; }

    /// <summary>سیستمی است و قابل حذف توسط tenant نیست (مثلا "Admin").</summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// لیست دسترسی‌های اختصاص‌یافته به این نقش (many-to-many با جدول واسط).
    /// </summary>
    public List<RolePermission> RolePermissions { get; set; } = new();
}

/// <summary>
/// پیوند نقش ↔ دسترسی.
/// </summary>
public class RolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RoleId { get; set; }

    public ApplicationRole? Role { get; set; }

    /// <summary>کلید دسترسی، یکی از <see cref="RbacApp.Domain.Enums.Permissions"/>.</summary>
    public string Permission { get; set; } = string.Empty;
}
