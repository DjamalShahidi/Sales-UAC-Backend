using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Domain.Exceptions;

namespace RbacApp.Infrastructure.Persistence.Tenant;

/// <summary>
/// ساخت TenantDbContext با connection string متناظر با tenant جاری (یا یک کانکشن دل‌خواه).
/// </summary>
public class TenantDbContextFactory : ITenantDbContextFactory
{
    private readonly ITenantContext _tenantContext;
    private readonly IServiceProvider _services;

    public TenantDbContextFactory(ITenantContext tenantContext, IServiceProvider services)
    {
        _tenantContext = tenantContext;
        _services = services;
    }

    /// <summary>
    /// اگر tenant در context نباشد استثنا پرتاب می‌کند.
    /// </summary>
    public ITenantDbContext CreateForCurrentTenant()
    {
        if (!_tenantContext.IsAvailable)
            throw new AppException(
                "هیچ tenant فعالی برای این درخواست resolve نشده است.", "no_tenant_context");

        return CreateForConnectionName(_tenantContext.ConnectionName);
    }

    public ITenantDbContext CreateForConnection(string connectionString)
        => Create(connectionString);

    /// <summary>
    /// connection string واقعی را از پیکربندی بر اساس نام می‌خواند.
    /// </summary>
    private ITenantDbContext CreateForConnectionName(string connectionName)
    {
        var configuration = _services.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString(connectionName)
            ?? throw new AppException(
                $"connection string با نام '{connectionName}' یافت نشد.", "connection_not_found");
        return Create(connectionString);
    }

    private TenantDbContext Create(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(TenantDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure();
            })
            .Options;

        return new TenantDbContext(options);
    }
}
