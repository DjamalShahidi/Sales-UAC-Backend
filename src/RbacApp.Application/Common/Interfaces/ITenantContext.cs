using RbacApp.Domain.Entities;

namespace RbacApp.Application.Common.Interfaces;

/// <summary>
/// context مربوط به tenant جاری در یک request.
/// توسط middleware برای هر request پر می‌شود.
/// </summary>
public interface ITenantContext
{
    /// <summary>true اگر tenant به‌درستی resolve شده و در دسترس است.</summary>
    bool IsAvailable { get; }

    /// <summary>شناسه‌ی tenant جاری.</summary>
    Guid TenantId { get; }

    /// <summary>slug مشخص‌کننده‌ی tenant.</summary>
    string Slug { get; }

    /// <summary>نام connection string در پیکربندی.</summary>
    string ConnectionName { get; }

    /// <summary>ارائه‌ی Tenant خوانده‌شده از Catalog.</summary>
    Tenant? Tenant { get; }

    /// <summary>true اگر درخواست متعلق به super admin است (بدون tenant).</summary>
    bool IsSuperAdminScope { get; }

    void SetTenant(Tenant tenant);
    void SetSuperAdminScope();
    void Clear();
}
