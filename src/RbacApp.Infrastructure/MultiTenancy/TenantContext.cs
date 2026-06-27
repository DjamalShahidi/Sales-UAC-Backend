using RbacApp.Application.Common.Interfaces;
using RbacApp.Domain.Entities;

namespace RbacApp.Infrastructure.MultiTenancy;

/// <summary>
/// پیاده‌سازی <see cref="ITenantContext"/> با عمر Scoped.
/// برای هر request یک نمونه ساخته می‌شود و توسط TenantResolverMiddleware پر می‌گردد.
/// </summary>
public class TenantContext : ITenantContext
{
    public bool IsAvailable { get; private set; }

    public Guid TenantId { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public string ConnectionName { get; private set; } = string.Empty;

    public Tenant? Tenant { get; private set; }

    public bool IsSuperAdminScope { get; private set; }

    public void SetTenant(Tenant tenant)
    {
        Tenant = tenant;
        TenantId = tenant.Id;
        Slug = tenant.Slug;
        ConnectionName = tenant.ConnectionName;
        IsAvailable = true;
        IsSuperAdminScope = false;
    }

    public void SetSuperAdminScope()
    {
        IsSuperAdminScope = true;
        IsAvailable = false;
    }

    public void Clear()
    {
        IsAvailable = false;
        IsSuperAdminScope = false;
        Tenant = null;
        TenantId = Guid.Empty;
        Slug = string.Empty;
        ConnectionName = string.Empty;
    }
}
