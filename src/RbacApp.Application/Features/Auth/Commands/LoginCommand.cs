using FluentValidation;
using MediatR;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Application.Common.Models;
using RbacApp.Domain.Exceptions;

namespace RbacApp.Application.Features.Auth.Commands;

public record LoginCommand(
    string TenantSlug,
    string Email,
    string Password,
    string? IpAddress) : IRequest<AuthTokenDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.TenantSlug).NotEmpty().MaximumLength(64)
            .Matches("^[a-z0-9-]+$").WithMessage("slug فقط می‌تواند شامل حروف کوچک، عدد و خط تیره باشد.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthTokenDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantDbContextFactory _dbFactory;
    private readonly IIdentityService _identity;
    private readonly IJwtTokenService _tokens;

    public LoginCommandHandler(
        ITenantContext tenantContext,
        ITenantDbContextFactory dbFactory,
        IIdentityService identity,
        IJwtTokenService tokens)
    {
        _tenantContext = tenantContext;
        _dbFactory = dbFactory;
        _identity = identity;
        _tokens = tokens;
    }

    public async Task<AuthTokenDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsAvailable)
            throw new TenantNotFoundException(request.TenantSlug);

        var tenant = _tenantContext.Tenant
            ?? throw new TenantNotFoundException(request.TenantSlug);

        if (tenant.Status == Domain.Enums.TenantStatus.Suspended)
            throw new AppException("این سازمان معلق شده است.", "tenant_suspended");

        var user = await _identity.FindUserByEmailAsync(request.Email, cancellationToken);
        // پیام خطای یکسان برای جلوگیری از user enumeration.
        if (user is null || !user.IsActive)
            throw new UnauthorizedException("ایمیل یا رمز عبور نادرست است.");

        var ok = await _identity.CheckPasswordAsync(user, request.Password, cancellationToken);
        if (!ok)
            throw new UnauthorizedException("ایمیل یا رمز عبور نادرست است.");

        var roles = await _identity.GetUserRolesAsync(user.Id, cancellationToken);
        var permissions = await _identity.GetUserPermissionsAsync(user.Id, cancellationToken);

        await _identity.UpdateLastLoginAsync(user.Id, cancellationToken);

        return await _tokens.GenerateForUserAsync(
            user.Id, user.Email!, user.FullName, roles, permissions, request.IpAddress, cancellationToken);
    }
}
