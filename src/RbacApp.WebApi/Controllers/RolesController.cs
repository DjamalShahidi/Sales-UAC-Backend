using Microsoft.AspNetCore.Mvc;
using MediatR;
using RbacApp.Application.Features.Roles.Commands;
using RbacApp.Application.Features.Roles.Queries;
using RbacApp.WebApi.Middleware;

namespace RbacApp.WebApi.Controllers;

[ApiController]
[Route("api/roles")]
[RequirePermission("roles.view")]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
        => _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Application.Dtos.RoleDto>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetRolesQuery());
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Application.Dtos.RoleDto), 200)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetRoleByIdQuery(id));
        return Ok(result);
    }

    [HttpGet("permissions")]
    [ProducesResponseType(typeof(IReadOnlyList<Application.Dtos.PermissionGroupDto>), 200)]
    public async Task<IActionResult> GetPermissions()
    {
        var result = await _mediator.Send(new GetPermissionGroupsQuery());
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission("roles.create")]
    [ProducesResponseType(typeof(Application.Dtos.RoleDto), 201)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request)
    {
        var result = await _mediator.Send(new CreateRoleCommand(
            request.Name, request.Description, request.Permissions));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("roles.update")]
    [ProducesResponseType(typeof(Application.Dtos.RoleDto), 200)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var result = await _mediator.Send(new UpdateRoleCommand(id, request.Description, request.Permissions));
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("roles.delete")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteRoleCommand(id));
        return NoContent();
    }
}
