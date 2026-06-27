using System.Security.Claims;

namespace RbacApp.Application.Common.Interfaces;

/// <summary>
/// اطلاعاتی از کاربر احرازشده‌ی جاری، استخراج‌شده از claims توکن.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    Guid? TenantId { get; }
    string? TenantSlug { get; }
    bool IsAuthenticated { get; }
    bool IsSuperAdmin { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }

    /// <summary>true اگر کاربر حداقل یکی از دسترسی‌ها را داشته باشد.</summary>
    bool HasPermission(string permission);

    /// <summary>true اگر کاربر در یکی از نقش‌ها باشد.</summary>
    bool IsInRole(string role);
}
