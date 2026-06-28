using Microsoft.AspNetCore.Mvc;
using MediatR;
using RbacApp.Application.Dtos;
using RbacApp.Application.Features.Tenants.Commands;
using RbacApp.Application.Features.Tenants.Queries;
using RbacApp.WebApi.Middleware;

namespace RbacApp.WebApi.Controllers;

[ApiController]
[Route("api/admin/tenants")]
[RequireRole("SuperAdmin")]
public class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantsController(IMediator mediator)
        => _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Application.Dtos.TenantSummaryDto>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetTenantsQuery());
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Application.Dtos.TenantDto), 200)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetTenantByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Application.Dtos.TenantDto), 201)]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request)
    {
        var result = await _mediator.Send(new CreateTenantCommand(
            request.Name, request.Slug, request.ConnectionName,
            request.Admin.FullName, request.Admin.Email, request.Admin.Password));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Application.Dtos.TenantDto), 200)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantRequest request)
    {
        var result = await _mediator.Send(new UpdateTenantCommand(id, request.Name, request.Status));
        return Ok(result);
    }
}
