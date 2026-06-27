using FluentValidation;
using MediatR;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Application.Common.Models;
using RbacApp.Domain.Exceptions;

namespace RbacApp.Application.Features.Auth.Commands;

public record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken,
    string? IpAddress) : IRequest<AuthTokenDto>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.AccessToken).NotEmpty();
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthTokenDto>
{
    private readonly IJwtTokenService _tokens;

    public RefreshTokenCommandHandler(IJwtTokenService tokens)
        => _tokens = tokens;

    public Task<AuthTokenDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        => _tokens.RotateRefreshTokenAsync(request.RefreshToken, request.IpAddress, cancellationToken);
}
