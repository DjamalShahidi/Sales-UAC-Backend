using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Application.Common.Models;
using RbacApp.Domain.Enums;
using RbacApp.Domain.Exceptions;

namespace RbacApp.Application.Features.Auth.Commands;

/// <summary>
/// ثبت اولین ادمین tenant. فقط زمانی مجاز است که هنوز هیچ کاربری در tenant وجود نداشته باشد.
/// نقش پیش‌فرض "Admin" با تمام دسترسی‌ها را به او اختصاص می‌دهد.
/// </summary>
public record RegisterFirstAdminCommand(
    string FullName,
    string Email,
    string Password,
    string? IpAddress) : IRequest<AuthTokenDto>;

public class RegisterFirstAdminCommandValidator : AbstractValidator<RegisterFirstAdminCommand>
{
    public RegisterFirstAdminCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty()
            .MinimumLength(8).WithMessage("رمز عبور باید حداقل ۸ کاراکتر باشد.")
            .Matches("[A-Z]").WithMessage("رمز عبور باید حداقل یک حرف بزرگ داشته باشد.")
            .Matches("[a-z]").WithMessage("رمز عبور باید حداقل یک حرف کوچک داشته باشد.")
            .Matches("[0-9]").WithMessage("رمز عبور باید حداقل یک عدد داشته باشد.");
    }
}

public class RegisterFirstAdminCommandHandler : IRequestHandler<RegisterFirstAdminCommand, AuthTokenDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly IIdentityService _identity;
    private readonly IJwtTokenService _tokens;
    private readonly ITenantDbContextFactory _dbFactory;

    public const string AdminRoleName = "Admin";

    public RegisterFirstAdminCommandHandler(
        ITenantContext tenantContext,
        IIdentityService identity,
        IJwtTokenService tokens,
        ITenantDbContextFactory dbFactory)
    {
        _tenantContext = tenantContext;
        _identity = identity;
        _tokens = tokens;
        _dbFactory = dbFactory;
    }

    public async Task<AuthTokenDto> Handle(RegisterFirstAdminCommand request, CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsAvailable)
            throw new TenantNotFoundException(_tenantContext.Slug ?? "unknown");

        // اطمینان از اینکه tenant خالی است — فقط اولین کاربر می‌تواند ادمین شود.
        var db = _dbFactory.CreateForCurrentTenant();
        await using (db)
        {
            if (await db.Users.AnyAsync(cancellationToken))
                throw new ConflictException("این سازمان قبلاً ادمین دارد.");

            var existing = await _identity.FindUserByEmailAsync(request.Email, cancellationToken);
            if (existing is not null)
                throw new ConflictException("این ایمیل قبلاً ثبت شده است.");

            var user = new Domain.Entities.Identity.ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                EmailConfirmed = true,
                IsActive = true
            };

            var (result, userId) = await _identity.CreateUserAsync(user, request.Password, cancellationToken);
            if (!result.Succeeded)
                throw new AppException(string.Join(" | ", result.Errors));

            await _identity.AddUserToRoleAsync(userId, AdminRoleName, cancellationToken);

            var roles = await _identity.GetUserRolesAsync(userId, cancellationToken);
            var permissions = await _identity.GetUserPermissionsAsync(userId, cancellationToken);

            await _identity.UpdateLastLoginAsync(userId, cancellationToken);

            return await _tokens.GenerateForUserAsync(
                userId, request.Email, request.FullName, roles, permissions, request.IpAddress, cancellationToken);
        }
    }
}
