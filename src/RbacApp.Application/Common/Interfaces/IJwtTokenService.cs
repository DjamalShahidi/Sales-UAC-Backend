using RbacApp.Application.Common.Models;

namespace RbacApp.Application.Common.Interfaces;

/// <summary>
/// تولید و اعتبارسنجی توکن‌های JWT و refresh.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// تولید access token و ثبت refresh token مرتبط در دیتابیس tenant.
    /// </summary>
    Task<AuthTokenDto> GenerateForUserAsync(
        Guid userId,
        string email,
        string fullName,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        string? ip,
        CancellationToken ct = default);

    /// <summary>
    /// با استفاده از refresh token، توکن جدید صادر و توکن قبلی باطل می‌شود (rotation).
    /// </summary>
    Task<AuthTokenDto> RotateRefreshTokenAsync(
        string refreshToken,
        string? ip,
        CancellationToken ct = default);

    /// <summary>
    /// باطل کردن تمام refresh tokenهای فعال یک کاربر (logout).
    /// </summary>
    Task RevokeUserTokensAsync(Guid userId, string reason, CancellationToken ct = default);

    /// <summary>
    /// claims اصلی توکن را از رشته‌ی آن استخراج می‌کند (برای اعتبارسنجی middleware).
    /// </summary>
    Task<JwtPrincipal?> GetPrincipalFromExpiredTokenAsync(string token);
}
