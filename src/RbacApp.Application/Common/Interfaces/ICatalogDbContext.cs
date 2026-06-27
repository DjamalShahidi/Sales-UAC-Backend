using Microsoft.EntityFrameworkCore;
using RbacApp.Domain.Entities;

namespace RbacApp.Application.Common.Interfaces;

/// <summary>
/// دیتابیس مرکزی (Catalog): رژیستری tenantها. فقط در scope مربوط به super admin استفاده می‌شود.
/// </summary>
public interface ICatalogDbContext : IDisposable, IAsyncDisposable
{
    DbSet<Tenant> Tenants { get; }

    Task<Tenant?> FindBySlugAsync(string slug, CancellationToken ct = default);

    Task<Tenant?> FindByIdAsync(Guid id, CancellationToken ct = default);

    Task<List<Tenant>> ListAsync(CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
