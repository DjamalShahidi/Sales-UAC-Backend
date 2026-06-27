namespace RbacApp.Application.Dtos;

public record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public bool IsActive { get; init; }
    public bool IsLockedOut { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}

public record UserListDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}

public record CreateUserRequest
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public IReadOnlyCollection<string> RoleNames { get; init; } = Array.Empty<string>();
    public bool IsActive { get; init; } = true;
}

public record UpdateUserRequest
{
    public string FullName { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public bool IsActive { get; init; }
}

public record AssignUserRolesRequest
{
    public IReadOnlyCollection<string> RoleNames { get; init; } = Array.Empty<string>();
}

public record ResetPasswordRequest
{
    public string NewPassword { get; init; } = string.Empty;
}
