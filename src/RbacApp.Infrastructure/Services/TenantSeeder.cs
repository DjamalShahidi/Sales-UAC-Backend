using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Domain.Entities.Identity;
using RbacApp.Domain.Enums;

namespace RbacApp.Infrastructure.Services;

/// <summary>
/// Seed اولیه‌ی دیتابیس tenant جدید:
/// - نقش "Admin" سیستمی با تمام دسترسی‌ها
/// - نقش "Viewer" سیستمی با فقط دسترسی‌های خواندن
/// - اولین ادمین tenant
/// مستقیماً روی DbContext کار می‌کند تا از پیچیدگی ساخت دستی UserManager جلوگیری شود.
/// </summary>
public class TenantSeeder : ITenantSeeder
{
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly ILogger<TenantSeeder> _logger;

    public TenantSeeder(IPasswordHasher<ApplicationUser> passwordHasher, ILogger<TenantSeeder> logger)
    {
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedRolesAsync(ITenantDbContext db, CancellationToken ct = default)
    {
        // نقش Admin — تمام دسترسی‌ها.
        await SeedRoleAsync(db, "Admin", "مدیر سیستم — تمام دسترسی‌ها", true, Permissions.All, ct);

        // نقش Viewer — فقط دسترسی‌های خواندن.
        await SeedRoleAsync(db, "Viewer", "مشاهده‌گر — فقط دسترسی خواندن", true,
            new[]
            {
                Permissions.DashboardView,
                Permissions.UsersView,
                Permissions.RolesView,
                Permissions.TenantsView
            }, ct);

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("نقش‌های سیستمی seed شدند.");
    }

    public async Task SeedFirstAdminAsync(
        ITenantDbContext db, string fullName, string email, string password, CancellationToken ct = default)
    {
        // اطمینان از وجود نقش Admin.
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin", ct);
        if (adminRole is null)
        {
            await SeedRolesAsync(db, ct);
            adminRole = await db.Roles.FirstAsync(r => r.Name == "Admin", ct);
        }

        // بررسی اینکه کاربر قبلاً وجود ندارد.
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (existing is not null)
        {
            _logger.LogWarning("ادمین اولیه {Email} قبلاً وجود دارد.", email);
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsActive = true,
            FullName = fullName,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant()
        };

        // هش رمز عبور با همان PasswordHasher که Identity استفاده می‌کند.
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        // stamp امنیتی برای invalidating توکن‌ها.
        user.SecurityStamp = Guid.NewGuid().ToString("N");

        db.Users.Add(user);

        // پیوند کاربر با نقش Admin (جدول AspNetUserRoles که Identity می‌سازد).
        db.Context.Add(new IdentityUserRole<Guid>
        {
            UserId = user.Id,
            RoleId = adminRole.Id
        });

        await db.Context.SaveChangesAsync(ct);
        _logger.LogInformation("ادمین اولیه {Email} ساخته شد.", email);
    }

    private static async Task SeedRoleAsync(
        ITenantDbContext db, string name, string description,
        bool isSystem, IEnumerable<string> perms, CancellationToken ct)
    {
        var exists = await db.Roles.AnyAsync(r => r.Name == name, ct);
        if (exists) return;

        var role = new ApplicationRole
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Description = description,
            IsSystem = isSystem
        };

        foreach (var perm in perms)
        {
            role.RolePermissions.Add(new RolePermission { Permission = perm });
        }

        db.Roles.Add(role);
    }
}
