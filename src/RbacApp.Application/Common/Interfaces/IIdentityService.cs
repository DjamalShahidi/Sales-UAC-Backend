using RbacApp.Domain.Entities.Identity;

namespace RbacApp.Application.Common.Interfaces;

/// <summary>
/// عملیات مدیریت کاربران و نقش‌ها که از ASP.NET Identity استفاده می‌کنند.
/// </summary>
public interface IIdentityService
{
    Task<ApplicationUser?> FindUserByEmailAsync(string email, CancellationToken ct = default);

    Task<ApplicationUser?> FindUserByIdAsync(Guid id, CancellationToken ct = default);

    Task<bool> CheckPasswordAsync(ApplicationUser user, string password, CancellationToken ct = default);

    Task<(IdentityResult Result, Guid UserId)> CreateUserAsync(
        ApplicationUser user,
        string password,
        CancellationToken ct = default);

    Task<IdentityResult> UpdateUserAsync(ApplicationUser user, CancellationToken ct = default);

    Task<IdentityResult> DeleteUserAsync(Guid userId, CancellationToken ct = default);

    Task<IdentityResult> AddUserToRoleAsync(Guid userId, string roleName, CancellationToken ct = default);

    Task<IdentityResult> RemoveUserFromRolesAsync(Guid userId, IEnumerable<string> roleNames, CancellationToken ct = default);

    Task<IList<string>> GetUserRolesAsync(Guid userId, CancellationToken ct = default);

    Task<IList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken ct = default);

    Task<bool> IsInRoleAsync(Guid userId, string roleName, CancellationToken ct = default);

    Task<IdentityResult> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default);

    Task UpdateLastLoginAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// همان IdentityResult مایکروسافت — برای کاهش وابستگی مستقیم در Domain بلااستفاده می‌رود،
/// ولی در Application مشکلی ندارد.
/// </summary>
public record IdentityResult(bool Succeeded, IEnumerable<string> Errors)
{
    public static IdentityResult Success() => new(true, Array.Empty<string>());
    public static IdentityResult Failure(params string[] errors) => new(false, errors);
}
