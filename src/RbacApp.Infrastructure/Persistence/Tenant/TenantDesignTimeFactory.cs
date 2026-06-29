using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RbacApp.Infrastructure.Persistence.Tenant;

/// <summary>
/// Factory زمان طراحی برای تولید migrationهای TenantDbContext.
/// چون TenantDbContext با کانکشن پویا در DI رجیستر نشده، این factory به EF
/// می‌گوید چگونه در زمان design-time یک نمونه بسازد.
/// </summary>
public class TenantDesignTimeFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlServer(
                "Server=localhost,1433;Database=RbacApp_Tenant_Template;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;",
                sql => sql.MigrationsAssembly(typeof(TenantDbContext).Assembly.FullName))
            .Options;

        return new TenantDbContext(options);
    }
}
