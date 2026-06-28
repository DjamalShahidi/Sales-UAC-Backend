using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

        // ---- Identity Options ----
        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.User.RequireUniqueEmail = true;
        });

        services.AddTransient<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();

        // ---- Catalog DB ----
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Catalog")!,
                sql => sql.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName)));

        services.AddScoped<ICatalogDbContext>(sp => sp.GetRequiredService<CatalogDbContext>());

        // ---- Tenant Context (scoped per request) ----
        services.AddScoped<ITenantContext, TenantContext>();

        // ---- Tenant DbContext Factory ----
        services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();

        // ---- Identity Managers Factory (برای tenant جاری) ----
        services.AddScoped<TenantIdentityContext>();

        // ---- Identity Service ----
        services.AddScoped<IIdentityService>(sp =>
        {
            var identityCtx = sp.GetRequiredService<TenantIdentityContext>();
            var db = sp.GetRequiredService<ITenantDbContextFactory>().CreateForCurrentTenant();
            identityCtx.Attach(db);
            return new IdentityService(identityCtx.UserManager, identityCtx.RoleManager, db);
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
