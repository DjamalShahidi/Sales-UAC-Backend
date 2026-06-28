using Microsoft.AspNetCore.Mvc;
using MediatR;
using RbacApp.Application.Dtos;
using RbacApp.Application.Features.Auth.Commands;

namespace RbacApp.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
        => _mediator = mediator;

    /// <summary>POST /api/auth/login</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(Application.Common.Models.AuthTokenDto), 200)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(new LoginCommand(
            request.TenantSlug, request.Email, request.Password, ip));
        return Ok(result);
    }

    /// <summary>POST /api/auth/refresh</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(Application.Common.Models.AuthTokenDto), 200)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(new RefreshTokenCommand(
            request.AccessToken, request.RefreshToken, ip));
        return Ok(result);
    }

    /// <summary>POST /api/auth/logout</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userIdStr = User.FindFirst("sub")?.Value;
        if (Guid.TryParse(userIdStr, out var userId))
            await _mediator.Send(new LogoutCommand(userId));
        return NoContent();
    }

    /// <summary>
    /// POST /api/auth/register-first-admin
    /// ثبت اولین ادمین tenant. فقط زمانی مجاز است که tenant خالی باشد.
    /// </summary>
    [HttpPost("register-first-admin")]
    [ProducesResponseType(typeof(Application.Common.Models.AuthTokenDto), 200)]
    public async Task<IActionResult> RegisterFirstAdmin([FromBody] RegisterFirstAdminRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(new RegisterFirstAdminCommand(
            request.FullName, request.Email, request.Password, ip));
        return Ok(result);
    }
}
