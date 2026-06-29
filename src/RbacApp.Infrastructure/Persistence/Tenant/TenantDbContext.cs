using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Domain.Entities.Identity;

namespace RbacApp.Infrastructure.Persistence.Tenant;

/// <summary>
/// دیتابیس مختص یک tenant: شامل Identity (کاربران/نقش‌ها) و جداول RBAC.
/// connection string آن در زمان ساخت تعیین می‌شود (per-tenant).
/// </summary>
public class TenantDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, ITenantDbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options) { }

    public new DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    public new DbSet<ApplicationRole> Roles => Set<ApplicationRole>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// دسترسی به DbContext واقعی برای عملیات پیشرفته (مثل migrate یا دسترسی به IdentityUserRole).
    /// </summary>
    DbContext ITenantDbContext.Context => this;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // پیشوند جداول Identity برای وضوح بیشتر.
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
                entity.SetTableName("App_" + tableName);
        }

        builder.ApplyConfiguration(new RolePermissionConfiguration());
        builder.ApplyConfiguration(new RefreshTokenConfiguration());
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("App_RolePermissions");
        b.HasKey(rp => rp.Id);

        b.Property(rp => rp.Permission).HasMaxLength(100).IsRequired();

        b.HasIndex(rp => new { rp.RoleId, rp.Permission }).IsUnique();

        b.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("App_RefreshTokens");
        b.HasKey(rt => rt.Id);

        b.Property(rt => rt.TokenHash).HasMaxLength(512).IsRequired();
        b.Property(rt => rt.RevokedReason).HasMaxLength(100);
        b.Property(rt => rt.CreatedByIp).HasMaxLength(50);
        b.Property(rt => rt.ReplacedByTokenHash).HasMaxLength(512);

        b.HasIndex(rt => rt.TokenHash);
        b.HasIndex(rt => rt.UserId);
    }
}
