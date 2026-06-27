namespace RbacApp.Application.Common.Models;

/// <summary>
/// خروجی عملیات احراز هویت: توکن‌ها و اطلاعات پایه‌ی کاربر.
/// </summary>
public record AuthTokenDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
    public string TokenType { get; init; } = "Bearer";

    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Permissions { get; init; } = Array.Empty<string>();
}

/// <summary>
/// claims استخراج‌شده از یک توکن (برای refresh).
/// </summary>
public record JwtPrincipal(
    Guid UserId,
    string Email,
    Guid TenantId,
    string TenantSlug,
    IReadOnlyCollection<string> Roles);
