using RbacApp.Domain.Common;

namespace RbacApp.Domain.Entities.Identity;

/// <summary>
/// توکن تمدید برای چرخش access token.
/// در دیتابیس tenant ذخیره می‌شود و در زمان خروج باطل می‌گردد.
/// </summary>
public class RefreshToken : BaseEntity
{
    /// <summary>شناسه‌ی کاربر مالک (AspNetUsers.Id).</summary>
    public Guid UserId { get; set; }

    /// <summary>هش رشته‌ی توکن — خود رشته هرگز ذخیره نمی‌شود.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>انقضا.</summary>
    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    /// <summary>دلیل ابطال (logout, rotate, breach).</summary>
    public string? RevokedReason { get; set; }

    public string? CreatedByIp { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>فعال است اگر نه باطل شده و نه منقضی.</summary>
    public bool IsActive => !IsRevoked && !IsExpired;
}
