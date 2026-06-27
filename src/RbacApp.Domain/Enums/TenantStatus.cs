namespace RbacApp.Domain.Enums;

/// <summary>
/// وضعیت چرخه‌ی حیات یک tenant.
/// </summary>
public enum TenantStatus
{
    /// <summary>فعال و قابل دسترسی.</summary>
    Active = 1,

    /// <summary>معلق شده — درخواست‌ها رد می‌شوند.</summary>
    Suspended = 2,

    /// <summary>پروژه‌سازی در حال انجام — فقط super admin دسترسی دارد.</summary>
    Provisioning = 3
}
