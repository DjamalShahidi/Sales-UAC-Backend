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
/// </summary>
public class TenantSeeder : ITenantSeeder
{
    private readonly ILogger<TenantSeeder> _logger;

    public TenantSeeder(ILogger<TenantSeeder> logger)
        => _logger = logger;

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
        // ساخت UserManager و RoleManager برای دیتابیس tenant.
        var context = db.Context;
        var userStore = new UserStore<ApplicationUser>(context);
        var roleStore = new RoleStore<ApplicationRole>(context);

        var userManager = new UserManager<ApplicationUser>(
            userStore, null!,
            new PasswordHasher<ApplicationUser>(),
            null, null, null, null, null, null!);
        userManager.Options.Password.RequireDigit = true;
        userManager.Options.Password.RequireLowercase = true;
        userManager.Options.Password.RequireUppercase = true;
        userManager.Options.Password.RequireNonAlphanumeric = false;
        userManager.Options.Password.RequiredLength = 8;

        var roleManager = new RoleManager<ApplicationRole>(
            roleStore, null!, null!, null!, null!);

        // مطمئن شویم نقش Admin وجود دارد.
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = "Admin",
                IsSystem = true,
                Description = "مدیر سیستم"
            });
        }

        // بررسی اینکه کاربر قبلاً وجود ندارد.
        var existing = await userManager.FindByEmailAsync(email);
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
            FullName = fullName
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
            throw new Domain.Exceptions.AppException($"ساخت ادمین اولیه ناموفق: {errors}");
        }

        await userManager.AddToRoleAsync(user, "Admin");
        await context.SaveChangesAsync(ct);

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
