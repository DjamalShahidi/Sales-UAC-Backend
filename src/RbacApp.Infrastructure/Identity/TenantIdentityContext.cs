using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Domain.Entities.Identity;

namespace RbacApp.Infrastructure.Identity;

/// <summary>
/// ساخت و نگه‌داری UserManager و RoleManager برای tenant جاری.
/// چون TenantDbContext با کانکشن پویا ساخته می‌شود، نمی‌توان از DI استاندارد Identity استفاده کرد.
/// این کلاس به‌صورت lazy، با اولین دسترسی، Managerها را می‌سازد.
/// </summary>
public class TenantIdentityContext
{
    private readonly IServiceProvider _services;
    private UserManager<ApplicationUser>? _userManager;
    private RoleManager<ApplicationRole>? _roleManager;
    private ITenantDbContext? _db;

    public TenantIdentityContext(IServiceProvider services)
        => _services = services;

    public UserManager<ApplicationUser> UserManager
        => _userManager ??= BuildUserManager();

    public RoleManager<ApplicationRole> RoleManager
        => _roleManager ??= BuildRoleManager();

    /// <summary>DbContext متصل به Managerها را تنظیم می‌کند (پیش از اولین دسترسی).</summary>
    public void Attach(ITenantDbContext db) => _db = db;

    private Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser, ApplicationRole, DbContext, Guid>
        CreateUserStore()
    {
        var db = _db ?? throw new InvalidOperationException(
            "TenantDbContext به TenantIdentityContext متصل نشده است. ابتدا Attach را فراخوانی کنید.");
        return new Microsoft.AspNetCore.Identity.EntityFrameworkCore
            .UserStore<ApplicationUser, ApplicationRole, DbContext, Guid>(db.Context);
    }

    private Microsoft.AspNetCore.Identity.EntityFrameworkCore.RoleStore<ApplicationRole, DbContext, Guid>
        CreateRoleStore()
    {
        var db = _db ?? throw new InvalidOperationException(
            "TenantDbContext به TenantIdentityContext متصل نشده است.");
        return new Microsoft.AspNetCore.Identity.EntityFrameworkCore
            .RoleStore<ApplicationRole, DbContext, Guid>(db.Context);
    }

    private UserManager<ApplicationUser> BuildUserManager()
    {
        var store = CreateUserStore();
        var options = _services.GetRequiredService<IOptions<IdentityOptions>>();
        var passwordHasher = _services.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        var userValidators = _services.GetService<IEnumerable<IUserValidator<ApplicationUser>>>()
            ?? Enumerable.Empty<IUserValidator<ApplicationUser>>();
        var passwordValidators = _services.GetService<IEnumerable<IPasswordValidator<ApplicationUser>>>()
            ?? Enumerable.Empty<IPasswordValidator<ApplicationUser>>();
        var normalizer = _services.GetRequiredService<ILookupNormalizer>();
        var describer = _services.GetRequiredService<IdentityErrorDescriber>();
        var loggerFactory = _services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();

        var manager = new UserManager<ApplicationUser>(
            store, options, passwordHasher,
            userValidators, passwordValidators,
            normalizer, describer, _services,
            loggerFactory.CreateLogger<UserManager<ApplicationUser>>());
        return manager;
    }

    private RoleManager<ApplicationRole> BuildRoleManager()
    {
        var store = CreateRoleStore();
        var roleValidators = _services.GetService<IEnumerable<IRoleValidator<ApplicationRole>>>()
            ?? Enumerable.Empty<IRoleValidator<ApplicationRole>>();
        var normalizer = _services.GetRequiredService<ILookupNormalizer>();
        var describer = _services.GetRequiredService<IdentityErrorDescriber>();
        var loggerFactory = _services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();

        return new RoleManager<ApplicationRole>(
            store, roleValidators, normalizer, describer,
            loggerFactory.CreateLogger<RoleManager<ApplicationRole>>());
    }
}
