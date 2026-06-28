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

    Task<(IdentityOutcome Result, Guid UserId)> CreateUserAsync(
        ApplicationUser user,
        string password,
        CancellationToken ct = default);

    Task<IdentityOutcome> UpdateUserAsync(ApplicationUser user, CancellationToken ct = default);

    Task<IdentityOutcome> DeleteUserAsync(Guid userId, CancellationToken ct = default);

    Task<IdentityOutcome> AddUserToRoleAsync(Guid userId, string roleName, CancellationToken ct = default);

    Task<IdentityOutcome> RemoveUserFromRolesAsync(Guid userId, IEnumerable<string> roleNames, CancellationToken ct = default);

    Task<IList<string>> GetUserRolesAsync(Guid userId, CancellationToken ct = default);

    Task<IList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken ct = default);

    Task<bool> IsInRoleAsync(Guid userId, string roleName, CancellationToken ct = default);

    Task<IdentityOutcome> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default);

    Task UpdateLastLoginAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// خلاصه‌ی نتیجه‌ی عملیات Identity، مستقل از نوع مایکروسافت.
/// </summary>
public record IdentityOutcome(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static IdentityOutcome Success() => new(true, Array.Empty<string>());
    public static IdentityOutcome Failure(params string[] errors) => new(false, errors);
}
