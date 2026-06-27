using MediatR;
using Microsoft.EntityFrameworkCore;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Application.Dtos;
using RbacApp.Domain.Enums;
using RbacApp.Domain.Exceptions;

namespace RbacApp.Application.Features.Roles.Queries;

public record GetRolesQuery() : IRequest<IReadOnlyList<RoleDto>>;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly ITenantDbContextFactory _factory;

    public GetRolesQueryHandler(ITenantDbContextFactory factory)
        => _factory = factory;

    public async Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        await using var db = _factory.CreateForCurrentTenant();

        var roles = await db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        // شمارش کاربران هر نقش — از جدول AspNetUserRoles استفاده می‌کنیم.
        return roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name!,
            Description = r.Description,
            IsSystem = r.IsSystem,
            Permissions = r.RolePermissions.Select(rp => rp.Permission).OrderBy(p => p).ToList()
        }).ToList();
    }
}

public record GetRoleByIdQuery(Guid Id) : IRequest<RoleDto>;

public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, RoleDto>
{
    private readonly ITenantDbContextFactory _factory;

    public GetRoleByIdQueryHandler(ITenantDbContextFactory factory)
        => _factory = factory;

    public async Task<RoleDto> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        await using var db = _factory.CreateForCurrentTenant();
        var role = await db.Roles.AsNoTracking()
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Role", request.Id);

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name!,
            Description = role.Description,
            IsSystem = role.IsSystem,
            Permissions = role.RolePermissions.Select(rp => rp.Permission).OrderBy(p => p).ToList()
        };
    }
}

/// <summary>لیست گروه‌بندی‌شده‌ی تمام دسترسی‌ها برای UI.</summary>
public record GetPermissionGroupsQuery : IRequest<IReadOnlyList<PermissionGroupDto>>;

public class GetPermissionGroupsQueryHandler
    : IRequestHandler<GetPermissionGroupsQuery, IReadOnlyList<PermissionGroupDto>>
{
    public Task<IReadOnlyList<PermissionGroupDto>> Handle(
        GetPermissionGroupsQuery request, CancellationToken cancellationToken)
    {
        var result = Permissions.Groups
            .Select(kv => new PermissionGroupDto
            {
                Group = kv.Key,
                Permissions = kv.Value.OrderBy(p => p).ToList()
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<PermissionGroupDto>>(result);
    }
}
