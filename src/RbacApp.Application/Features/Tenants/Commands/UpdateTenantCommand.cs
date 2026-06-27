using FluentValidation;
using MediatR;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Application.Dtos;
using RbacApp.Domain.Enums;
using RbacApp.Domain.Exceptions;

namespace RbacApp.Application.Features.Tenants.Commands;

public record UpdateTenantCommand(
    Guid Id,
    string Name,
    TenantStatus Status) : IRequest<TenantDto>;

public class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}

public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, TenantDto>
{
    private readonly ICatalogDbContext _catalog;

    public UpdateTenantCommandHandler(ICatalogDbContext catalog)
        => _catalog = catalog;

    public async Task<TenantDto> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _catalog.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Tenant", request.Id);

        tenant.Name = request.Name;
        tenant.Status = request.Status;
        tenant.UpdatedAt = DateTime.UtcNow;

        await using (_catalog)
        {
            await _catalog.SaveChangesAsync(cancellationToken);
        }

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            ConnectionName = tenant.ConnectionName,
            AdminEmail = tenant.AdminEmail,
            Status = tenant.Status,
            CreatedAt = tenant.CreatedAt,
            LastAccessedAt = tenant.LastAccessedAt
        };
    }
}
