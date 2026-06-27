using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Application.Dtos;
using RbacApp.Domain.Entities.Identity;
using RbacApp.Domain.Enums;
using RbacApp.Domain.Exceptions;

namespace RbacApp.Application.Features.Roles.Commands;

// ---------- Create ----------
public record CreateRoleCommand(string Name, string? Description, IReadOnlyCollection<string> Permissions)
    : IRequest<RoleDto>;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, RoleDto>
{
    private readonly ITenantDbContextFactory _factory;

    public CreateRoleCommandHandler(ITenantDbContextFactory factory)
        => _factory = factory;

    public async Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        await using var db = _factory.CreateForCurrentTenant();

        var exists = await db.Roles.AnyAsync(r => r.Name == request.Name, cancellationToken);
        if (exists)
            throw new ConflictException($"نقش '{request.Name}' قبلاً وجود دارد.");

        var invalid = request.Permissions.Except(Permissions.All).ToList();
        if (invalid.Count > 0)
            throw new AppException($"دسترسی نامعتبر: {string.Join(", ", invalid)}");

        var role = new ApplicationRole
        {
            Name = request.Name,
            Description = request.Description,
            IsSystem = false
        };
        foreach (var p in request.Permissions.Distinct())
            role.RolePermissions.Add(new RolePermission { Permission = p });

        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name!,
            Description = role.Description,
            IsSystem = role.IsSystem,
            Permissions = role.RolePermissions.Select(rp => rp.Permission).ToList()
        };
    }
}

// ---------- Update (permissions) ----------
public record UpdateRoleCommand(Guid Id, string? Description, IReadOnlyCollection<string> Permissions)
    : IRequest<RoleDto>;

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, RoleDto>
{
    private readonly ITenantDbContextFactory _factory;

    public UpdateRoleCommandHandler(ITenantDbContextFactory factory)
        => _factory = factory;

    public async Task<RoleDto> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        await using var db = _factory.CreateForCurrentTenant();

        var role = await db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Role", request.Id);

        if (role.IsSystem && request.Description is null)
            throw new ConflictException("نقش سیستمی قابل تغییر نیست.");

        var invalid = request.Permissions.Except(Permissions.All).ToList();
        if (invalid.Count > 0)
            throw new AppException($"دسترسی نامعتبر: {string.Join(", ", invalid)}");

        if (request.Description is not null)
            role.Description = request.Description;

        // بازنویسی دسترسی‌ها.
        var current = role.RolePermissions.Select(rp => rp.Permission).ToList();
        var toAdd = request.Permissions.Except(current).ToList();
        var toRemove = current.Except(request.Permissions).ToList();

        foreach (var p in toAdd)
            role.RolePermissions.Add(new RolePermission { Permission = p, RoleId = role.Id });
        foreach (var p in toRemove)
        {
            var entity = role.RolePermissions.First(rp => rp.Permission == p);
            role.RolePermissions.Remove(entity);
            db.Context.Set<RolePermission>().Remove(entity);
        }

        await db.SaveChangesAsync(cancellationToken);

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

// ---------- Delete ----------
public record DeleteRoleCommand(Guid Id) : IRequest<Unit>;

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Unit>
{
    private readonly ITenantDbContextFactory _factory;

    public DeleteRoleCommandHandler(ITenantDbContextFactory factory)
        => _factory = factory;

    public async Task<Unit> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        await using var db = _factory.CreateForCurrentTenant();
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Role", request.Id);

        if (role.IsSystem)
            throw new ConflictException("نقش سیستمی قابل حذف نیست.");

        // بررسی اینکه کاربری این نقش را دارد یا نه — از AspNetUserRoles.
        var userInRole = await db.Context.Set<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>()
            .AnyAsync(ur => ur.RoleId == request.Id, cancellationToken);
        if (userInRole)
            throw new ConflictException("این نقش به کاربرانی اختصاص دارد و قابل حذف نیست.");

        db.Roles.Remove(role);
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
