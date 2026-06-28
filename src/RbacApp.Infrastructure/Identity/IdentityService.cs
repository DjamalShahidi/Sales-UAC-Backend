using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Domain.Entities.Identity;

namespace RbacApp.Infrastructure.Identity;

/// <summary>
/// پیاده‌سازی IIdentityService با استفاده از UserManager و RoleManager.
/// برای هر tenant یک نمونه جداگانه ساخته می‌شود (scoped به TenantDbContext).
/// </summary>
public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ITenantDbContext _db;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ITenantDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    public async Task<ApplicationUser?> FindUserByEmailAsync(string email, CancellationToken ct = default)
        => await _userManager.FindByEmailAsync(email);

    public async Task<ApplicationUser?> FindUserByIdAsync(Guid id, CancellationToken ct = default)
        => await _userManager.FindByIdAsync(id.ToString());

    public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password, CancellationToken ct = default)
        => await _userManager.CheckPasswordAsync(user, password);

    public async Task<(IdentityOutcome Result, Guid UserId)> CreateUserAsync(
        ApplicationUser user, string password, CancellationToken ct = default)
    {
        var result = await _userManager.CreateAsync(user, password);
        return (MapResult(result), user.Id);
    }

    public async Task<IdentityOutcome> UpdateUserAsync(ApplicationUser user, CancellationToken ct = default)
        => MapResult(await _userManager.UpdateAsync(user));

    public async Task<IdentityOutcome> DeleteUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await FindUserByIdAsync(userId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("User", userId);
        return MapResult(await _userManager.DeleteAsync(user));
    }

    public async Task<IdentityOutcome> AddUserToRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        var user = await FindUserByIdAsync(userId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("User", userId);

        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new ApplicationRole
            {
                Name = roleName,
                IsSystem = false
            });
        }

        return MapResult(await _userManager.AddToRoleAsync(user, roleName));
    }

    public async Task<IdentityOutcome> RemoveUserFromRolesAsync(
        Guid userId, IEnumerable<string> roleNames, CancellationToken ct = default)
    {
        var user = await FindUserByIdAsync(userId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("User", userId);
        return MapResult(await _userManager.RemoveFromRolesAsync(user, roleNames));
    }

    public async Task<IList<string>> GetUserRolesAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await FindUserByIdAsync(userId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("User", userId);
        return await _userManager.GetRolesAsync(user);
    }

    public async Task<IList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await FindUserByIdAsync(userId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("User", userId);

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count == 0)
            return Array.Empty<string>();

        var roleIds = await _db.Roles
            .Where(r => roles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync(ct);

        return await _db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<bool> IsInRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        var user = await FindUserByIdAsync(userId, ct);
        return user is not null && await _userManager.IsInRoleAsync(user, roleName);
    }

    public async Task<IdentityOutcome> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default)
    {
        var user = await FindUserByIdAsync(userId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("User", userId);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        return MapResult(await _userManager.ResetPasswordAsync(user, token, newPassword));
    }

    public async Task UpdateLastLoginAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await FindUserByIdAsync(userId, ct);
        if (user is not null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }
    }

    private static IdentityOutcome MapResult(Microsoft.AspNetCore.Identity.IdentityResult result)
    {
        return result.Succeeded
            ? IdentityOutcome.Success()
            : IdentityOutcome.Failure(
                result.Errors.Select(e => e.Description).ToArray());
    }
}
