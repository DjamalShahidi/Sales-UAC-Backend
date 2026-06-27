using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Application.Dtos;
using RbacApp.Domain.Entities.Identity;
using RbacApp.Domain.Exceptions;

namespace RbacApp.Application.Features.Users.Commands;

// ---------- Create ----------
public record CreateUserCommand(
    string FullName,
    string Email,
    string Password,
    IReadOnlyCollection<string> RoleNames,
    bool IsActive = true) : IRequest<UserDto>;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.RoleNames).NotEmpty().WithMessage("حداقل یک نقش باید اختصاص یابد.");
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly ITenantDbContextFactory _factory;
    private readonly IIdentityService _identity;

    public CreateUserCommandHandler(ITenantDbContextFactory factory, IIdentityService identity)
    {
        _factory = factory;
        _identity = identity;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await _identity.FindUserByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            throw new ConflictException("این ایمیل قبلاً ثبت شده است.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            IsActive = request.IsActive,
            FullName = request.FullName
        };

        var (result, userId) = await _identity.CreateUserAsync(user, request.Password, cancellationToken);
        if (!result.Succeeded)
            throw new AppException(string.Join(" | ", result.Errors));

        foreach (var role in request.RoleNames.Distinct())
            await _identity.AddUserToRoleAsync(userId, role, cancellationToken);

        var roles = await _identity.GetUserRolesAsync(userId, cancellationToken);
        var created = await _identity.FindUserByIdAsync(userId, cancellationToken);

        return new UserDto
        {
            Id = userId,
            Email = created!.Email!,
            UserName = created.UserName!,
            FullName = created.FullName,
            IsActive = created.IsActive,
            IsLockedOut = false,
            CreatedAt = created.CreatedAt,
            Roles = roles.ToList()
        };
    }
}

// ---------- Update ----------
public record UpdateUserCommand(Guid Id, string FullName, string? DisplayName, bool IsActive)
    : IRequest<Unit>;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
    }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Unit>
{
    private readonly IIdentityService _identity;

    public UpdateUserCommandHandler(IIdentityService identity)
        => _identity = identity;

    public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _identity.FindUserByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("User", request.Id);

        user.FullName = request.FullName;
        user.DisplayName = request.DisplayName;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await _identity.UpdateUserAsync(user, cancellationToken);
        if (!result.Succeeded)
            throw new AppException(string.Join(" | ", result.Errors));

        return Unit.Value;
    }
}

// ---------- Assign roles ----------
public record AssignUserRolesCommand(Guid Id, IReadOnlyCollection<string> RoleNames) : IRequest<Unit>;

public class AssignUserRolesCommandValidator : AbstractValidator<AssignUserRolesCommand>
{
    public AssignUserRolesCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class AssignUserRolesCommandHandler : IRequestHandler<AssignUserRolesCommand, Unit>
{
    private readonly IIdentityService _identity;

    public AssignUserRolesCommandHandler(IIdentityService identity)
        => _identity = identity;

    public async Task<Unit> Handle(AssignUserRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await _identity.FindUserByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("User", request.Id);

        var currentRoles = await _identity.GetUserRolesAsync(request.Id, cancellationToken);
        if (currentRoles.Count > 0)
            await _identity.RemoveUserFromRolesAsync(request.Id, currentRoles, cancellationToken);

        foreach (var role in request.RoleNames.Distinct())
            await _identity.AddUserToRoleAsync(request.Id, role, cancellationToken);

        return Unit.Value;
    }
}

// ---------- Toggle active ----------
public record ToggleUserActiveCommand(Guid Id, bool IsActive) : IRequest<Unit>;

public class ToggleUserActiveCommandHandler : IRequestHandler<ToggleUserActiveCommand, Unit>
{
    private readonly IIdentityService _identity;

    public ToggleUserActiveCommandHandler(IIdentityService identity)
        => _identity = identity;

    public async Task<Unit> Handle(ToggleUserActiveCommand request, CancellationToken cancellationToken)
    {
        var user = await _identity.FindUserByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("User", request.Id);

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _identity.UpdateUserAsync(user, cancellationToken);
        return Unit.Value;
    }
}

// ---------- Reset password ----------
public record ResetPasswordCommand(Guid Id, string NewPassword) : IRequest<Unit>;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Unit>
{
    private readonly IIdentityService _identity;

    public ResetPasswordCommandHandler(IIdentityService identity)
        => _identity = identity;

    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _identity.ResetPasswordAsync(request.Id, request.NewPassword, cancellationToken);
        if (!result.Succeeded)
            throw new AppException(string.Join(" | ", result.Errors));
        return Unit.Value;
    }
}

// ---------- Delete ----------
public record DeleteUserCommand(Guid Id) : IRequest<Unit>;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IIdentityService _identity;
    private readonly ICurrentUserService _current;

    public DeleteUserCommandHandler(IIdentityService identity, ICurrentUserService current)
    {
        _identity = identity;
        _current = current;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        if (_current.UserId == request.Id)
            throw new ConflictException("نمی‌توانید حساب کاربری خودتان را حذف کنید.");

        var result = await _identity.DeleteUserAsync(request.Id, cancellationToken);
        if (!result.Succeeded)
            throw new AppException(string.Join(" | ", result.Errors));
        return Unit.Value;
    }
}
