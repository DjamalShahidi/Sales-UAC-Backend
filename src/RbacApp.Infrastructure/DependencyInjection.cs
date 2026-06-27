using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Domain.Entities.Identity;
using RbacApp.Infrastructure.Identity;
using RbacApp.Infrastructure.MultiTenancy;
using RbacApp.Infrastructure.Persistence.Catalog;
using RbacApp.Infrastructure.Persistence.Tenant;
using RbacApp.Infrastructure.Services;

namespace RbacApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ---- Memory Cache ----
        services.AddMemoryCache();

        // ---- Catalog DB ----
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Catalog")!,
                sql => sql.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName)));

        services.AddScoped<ICatalogDbContext>(sp =>
        {
            var db = sp.GetRequiredService<CatalogDbContext>();
            return db;
        });

        // ---- Tenant Context (scoped per request) ----
        services.AddScoped<ITenantContext, TenantContext>();

        // ---- Tenant DbContext Factory ----
        services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();

        // ---- Identity for Tenant (کدنویسی شده به‌صورت factory) ----
        // UserManager و RoleManager برای tenant: باید با DbContext پویا ساخته شوند.
        // یک factory برای ساخت UserManager و RoleManager با TenantDbContext.
        services.AddScoped(sp =>
        {
            var tenantCtx = sp.GetRequiredService<ITenantContext>();
            var factory = sp.GetRequiredService<ITenantDbContextFactory>();

            if (!tenantCtx.IsAvailable)
                throw new System.InvalidOperationException("tenant context در دسترس نیست.");

            var db = factory.CreateForCurrentTenant();

            var userStore = new UserStore<ApplicationUser>(db.Context);
            var roleStore = new RoleStore<ApplicationRole>(db.Context);

            var userManager = new UserManager<ApplicationUser>(
                userStore, sp.GetRequiredService<IOptions<IdentityOptions>>(),
                sp.GetRequiredService<IPasswordHasher<ApplicationUser>>(),
                sp.GetRequiredService<IEnumerable<IUserValidator<ApplicationUser>>>(),
                sp.GetRequiredService<IEnumerable<IPasswordValidator<ApplicationUser>>>(),
                sp.GetRequiredService<ILookupNormalizer>(),
                sp.GetRequiredService<IdentityErrorDescriber>(),
                sp.GetRequiredService<IServiceProvider>());

            var roleManager = new RoleManager<ApplicationRole>(
                roleStore, sp.GetRequiredService<IRoleValidator<ApplicationRole>>(),
                sp.GetRequiredService<ILookupNormalizer>(),
                sp.GetRequiredService<IdentityErrorDescriber>(),
                sp.GetRequiredService<ILoggerFactory>());

            return (UserManager: userManager, RoleManager: roleManager, Db: db);
        });

        // ---- Identity Service ----
        services.AddScoped<IIdentityService>(sp =>
        {
            var (userManager, roleManager, db) = sp.GetRequiredService<
                (UserManager<ApplicationUser> UserManager,
                 RoleManager<ApplicationRole> RoleManager,
                 ITenantDbContext Db)>();
            return new IdentityService(userManager, roleManager, db);
        });

        // ---- JWT Token Service ----
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // ---- Current User ----
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // ---- DB Provisioner ----
        services.AddScoped<ITenantDbProvisioner, TenantDbProvisioner>();

        // ---- Tenant Seeder ----
        services.AddScoped<ITenantSeeder, TenantSeeder>();

        // ---- JWT Authentication ----
        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key تنظیم نشده است.");
        var issuer = configuration["Jwt:Issuer"] ?? "RbacApp";
        var audience = configuration["Jwt:Audience"] ?? "RbacApp";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

        return services;
    }
}
