namespace RbacApp.Application.Dtos;

public record RoleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
    public int UserCount { get; init; }
    public IReadOnlyCollection<string> Permissions { get; init; } = Array.Empty<string>();
}

public record CreateRoleRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IReadOnlyCollection<string> Permissions { get; init; } = Array.Empty<string>();
}

public record UpdateRoleRequest
{
    public string? Description { get; init; }
    public IReadOnlyCollection<string> Permissions { get; init; } = Array.Empty<string>();
}

/// <summary>گروه‌بندی دسترسی‌ها برای نمایش ماتریس در UI.</summary>
public record PermissionGroupDto
{
    public string Group { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Permissions { get; init; } = Array.Empty<string>();
}
