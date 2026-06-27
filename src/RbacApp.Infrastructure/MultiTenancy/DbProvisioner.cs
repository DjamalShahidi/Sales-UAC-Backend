using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Infrastructure.Persistence.Tenant;

namespace RbacApp.Infrastructure.MultiTenancy;

/// <summary>
/// ایجاد و migrate دیتابیس فیزیکی برای tenant جدید.
/// </summary>
public class TenantDbProvisioner : ITenantDbProvisioner
{
    private readonly ILogger<TenantDbProvisioner> _logger;

    public TenantDbProvisioner(ILogger<TenantDbProvisioner> logger)
        => _logger = logger;

    public async Task ProvisionAsync(string connectionString, CancellationToken ct = default)
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(TenantDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure();
            })
            .Options;

        await using var context = new TenantDbContext(options);

        _logger.LogInformation("ایجاد دیتابیس tenant...");
        await context.Database.EnsureCreatedAsync(ct);

        _logger.LogInformation("اعمال migrationهای معلق...");
        var pending = await context.Database.GetPendingMigrationsAsync(ct);
        foreach (var migration in pending)
        {
            _logger.LogDebug("اعمال migration: {Migration}", migration);
        }
        await context.Database.MigrateAsync(ct);

        _logger.LogInformation("دیتابیس tenant آماده شد.");
    }
}
