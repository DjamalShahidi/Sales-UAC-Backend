using MediatR;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Domain.Exceptions;

namespace RbacApp.Application.Features.Auth.Commands;

public record LogoutCommand(Guid UserId) : IRequest<Unit>;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly ICurrentUserService _current;
    private readonly IJwtTokenService _tokens;

    public LogoutCommandHandler(ICurrentUserService current, IJwtTokenService tokens)
    {
        _current = current;
        _tokens = tokens;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // فقط کاربر خودش می‌تواند خود را logout کند.
        if (_current.UserId != request.UserId)
            throw new UnauthorizedException();

        await _tokens.RevokeUserTokensAsync(request.UserId, "logout", cancellationToken);
        return Unit.Value;
    }
}
