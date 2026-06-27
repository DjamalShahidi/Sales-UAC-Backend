using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Application.Common.Models;
using RbacApp.Application.Dtos;
using RbacApp.Domain.Exceptions;

namespace RbacApp.Application.Features.Users.Queries;

public record GetUsersQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<PagedResult<UserListDto>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserListDto>>
{
    private readonly ITenantDbContextFactory _factory;
    private readonly IIdentityService _identity;

    public GetUsersQueryHandler(ITenantDbContextFactory factory, IIdentityService identity)
    {
        _factory = factory;
        _identity = identity;
    }

    public async Task<PagedResult<UserListDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 100);
        var skip = (page - 1) * size;

        await using var db = _factory.CreateForCurrentTenant();

        var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(u =>
                (u.FullName != null && u.FullName.Contains(s)) ||
                (u.Email != null && u.Email.Contains(s)) ||
                (u.UserName != null && u.UserName.Contains(s)));
        }

        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip(skip)
            .Take(size)
            .ToListAsync(cancellationToken);

        var items = new List<UserListDto>();
        foreach (var u in users)
        {
            var roles = await _identity.GetUserRolesAsync(u.Id, cancellationToken);
            items.Add(new UserListDto
            {
                Id = u.Id,
                Email = u.Email!,
                FullName = u.FullName,
                IsActive = u.IsActive,
                LastLoginAt = u.LastLoginAt,
                Roles = roles.ToList()
            });
        }

        return new PagedResult<UserListDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = size
        };
    }
}

public record GetUserByIdQuery(Guid Id) : IRequest<UserDto>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly ITenantDbContextFactory _factory;
    private readonly IIdentityService _identity;

    public GetUserByIdQueryHandler(ITenantDbContextFactory factory, IIdentityService identity)
    {
        _factory = factory;
        _identity = identity;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        await using var db = _factory.CreateForCurrentTenant();
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("User", request.Id);

        var roles = await _identity.GetUserRolesAsync(user.Id, cancellationToken);
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            UserName = user.UserName!,
            FullName = user.FullName,
            DisplayName = user.DisplayName,
            IsActive = user.IsActive,
            IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = roles.ToList()
        };
    }
}
