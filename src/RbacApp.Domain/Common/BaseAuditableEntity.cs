namespace RbacApp.Domain.Common;

/// <summary>
/// موجودیت قابل حذف نرم (Soft Delete).
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity
{
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }
}
