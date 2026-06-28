using Microsoft.AspNetCore.Mvc;
using MediatR;
using RbacApp.Application.Dtos;
using RbacApp.Application.Features.Users.Commands;
using RbacApp.Application.Features.Users.Queries;
using RbacApp.WebApi.Middleware;

namespace RbacApp.WebApi.Controllers;

[ApiController]
[Route("api/users")]
[RequirePermission("users.view")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
        => _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(Application.Common.Models.PagedResult<Application.Dtos.UserListDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var result = await _mediator.Send(new GetUsersQuery(page, pageSize, search));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Application.Dtos.UserDto), 200)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission("users.create")]
    [ProducesResponseType(typeof(Application.Dtos.UserDto), 201)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var result = await _mediator.Send(new CreateUserCommand(
            request.FullName, request.Email, request.Password,
            request.RoleNames, request.IsActive));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("users.update")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        await _mediator.Send(new UpdateUserCommand(id, request.FullName, request.DisplayName, request.IsActive));
        return NoContent();
    }

    [HttpPut("{id:guid}/roles")]
    [RequirePermission("users.manage_roles")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignUserRolesRequest request)
    {
        await _mediator.Send(new AssignUserRolesCommand(id, request.RoleNames));
        return NoContent();
    }

    [HttpPatch("{id:guid}/toggle-active")]
    [RequirePermission("users.update")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> ToggleActive(Guid id, [FromBody] bool isActive)
    {
        await _mediator.Send(new ToggleUserActiveCommand(id, isActive));
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    [RequirePermission("users.update")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request)
    {
        await _mediator.Send(new ResetPasswordCommand(id, request.NewPassword));
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("users.delete")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteUserCommand(id));
        return NoContent();
    }
}
