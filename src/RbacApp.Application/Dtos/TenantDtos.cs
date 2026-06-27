using RbacApp.Domain.Enums;

namespace RbacApp.Application.Dtos;

public record TenantDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string ConnectionName { get; init; } = string.Empty;
    public string? AdminEmail { get; init; }
    public TenantStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastAccessedAt { get; init; }
}

public record TenantSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public TenantStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record UpdateTenantRequest
{
    public string Name { get; init; } = string.Empty;
    public TenantStatus Status { get; init; }
}
