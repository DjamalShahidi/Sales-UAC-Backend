using MediatR;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Application.Dtos;

namespace RbacApp.Application.Features.Tenants.Queries;

public record GetTenantsQuery : IRequest<IReadOnlyList<TenantSummaryDto>>;

public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, IReadOnlyList<TenantSummaryDto>>
{
    private readonly ICatalogDbContext _catalog;

    public GetTenantsQueryHandler(ICatalogDbContext catalog)
        => _catalog = catalog;

    public async Task<IReadOnlyList<TenantSummaryDto>> Handle(
        GetTenantsQuery request, CancellationToken cancellationToken)
    {
        await using (_catalog)
        {
            var tenants = await _catalog.ListAsync(cancellationToken);
            return tenants
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TenantSummaryDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Slug = t.Slug,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt
                })
                .ToList();
        }
    }
}

public record GetTenantByIdQuery(Guid Id) : IRequest<TenantDto>;

public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, TenantDto>
{
    private readonly ICatalogDbContext _catalog;

    public GetTenantByIdQueryHandler(ICatalogDbContext catalog)
        => _catalog = catalog;

    public async Task<TenantDto> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _catalog.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new Domain.Exceptions.NotFoundException("Tenant", request.Id);

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
