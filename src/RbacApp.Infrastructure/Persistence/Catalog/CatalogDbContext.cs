using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RbacApp.Application.Common.Interfaces;
using TenantEntity = RbacApp.Domain.Entities.Tenant;

namespace RbacApp.Infrastructure.Persistence.Catalog;

/// <summary>
/// دیتابیس مرکزی (Catalog): رژیستری tenantها.
/// یک کانکشن ثابت دارد (در پیکربندی با نام "Catalog") و در scope super admin استفاده می‌شود.
/// </summary>
public class CatalogDbContext : DbContext, ICatalogDbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();

    public async Task<TenantEntity?> FindBySlugAsync(string slug, CancellationToken ct = default)
        => await Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == slug, ct);

    public async Task<TenantEntity?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => await Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<List<TenantEntity>> ListAsync(CancellationToken ct = default)
        => await Tenants.AsNoTracking().ToListAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

public class TenantConfiguration : IEntityTypeConfiguration<TenantEntity>
{
    public void Configure(EntityTypeBuilder<TenantEntity> b)
    {
        b.ToTable("Tenants");
        b.HasKey(t => t.Id);

        b.Property(t => t.Name).HasMaxLength(150).IsRequired();
        b.Property(t => t.Slug).HasMaxLength(64).IsRequired();
        b.Property(t => t.ConnectionName).HasMaxLength(100).IsRequired();
        b.Property(t => t.AdminEmail).HasMaxLength(256);
        b.Property(t => t.Status).HasConversion<int>();

        b.HasIndex(t => t.Slug).IsUnique();
    }
}
