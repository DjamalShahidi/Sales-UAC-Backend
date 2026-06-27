namespace RbacApp.Domain.Common;

/// <summary>
/// پایه‌ی همه‌ی موجودیت‌ها؛ شامل کلید و فیلدهای ممیزی.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }
}
