using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RbacApp.Application.Common.Interfaces;
using RbacApp.Application.Dtos;
using RbacApp.Domain.Entities;
using RbacApp.Domain.Enums;
using RbacApp.Domain.Exceptions;

namespace RbacApp.Application.Features.Tenants.Commands;

/// <summary>
/// ساخت tenant جدید (تنها توسط super admin):
/// 1) رکورد tenant در Catalog ثبت می‌شود (وضعیت Provisioning).
/// 2) دیتابیس فیزیکی tenant provision و migrate می‌شود.
/// 3) نقش پیش‌فرض "Admin" با تمام دسترسی‌ها seed می‌شود.
/// 4) اولین ادمین tenant ساخته می‌شود.
/// 5) وضعیت tenant روی Active تنظیم می‌شود.
/// </summary>
public record CreateTenantCommand(
    string Name,
    string Slug,
    string ConnectionName,
    string AdminFullName,
    string AdminEmail,
    string AdminPassword) : IRequest<TenantDto>;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(64)
            .Matches("^[a-z0-9-]+$").WithMessage("slug فقط شامل حروف کوچک، عدد و خط تیره.");
        RuleFor(x => x.ConnectionName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminFullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.AdminPassword).NotEmpty().MinimumLength(8);
    }
}

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, TenantDto>
{
    private readonly ICatalogDbContext _catalog;
    private readonly ITenantDbProvisioner _provisioner;
    private readonly ITenantSeeder _seeder;
    private readonly IConfiguration _configuration;
    private readonly ITenantDbContextFactory _tenantDbFactory;
    private readonly ILogger<CreateTenantCommandHandler> _logger;

    public CreateTenantCommandHandler(
        ICatalogDbContext catalog,
        ITenantDbProvisioner provisioner,
        ITenantSeeder seeder,
        IConfiguration configuration,
        ITenantDbContextFactory tenantDbFactory,
        ILogger<CreateTenantCommandHandler> logger)
    {
        _catalog = catalog;
        _provisioner = provisioner;
        _seeder = seeder;
        _configuration = configuration;
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    public async Task<TenantDto> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        // 1) بررسی یکتایی slug در Catalog.
        var existing = await _catalog.FindBySlugAsync(request.Slug, cancellationToken);
        if (existing is not null)
            throw new ConflictException($"tenant با slug '{request.Slug}' قبلاً وجود دارد.");

        // 2) دریافت connection string واقعی از پیکربندی.
        var connectionString = _configuration.GetConnectionString(request.ConnectionName)
            ?? throw new AppException(
                $"connection string با نام '{request.ConnectionName}' در پیکربندی یافت نشد.",
                "connection_not_found");

        // 3) ثبت رکورد در Catalog با وضعیت Provisioning.
        var tenant = new Tenant
        {
            Name = request.Name,
            Slug = request.Slug,
            ConnectionName = request.ConnectionName,
            AdminEmail = request.AdminEmail,
            Status = TenantStatus.Provisioning
        };

        await using (_catalog)
        {
            await _catalog.Tenants.AddAsync(tenant, cancellationToken);
            await _catalog.SaveChangesAsync(cancellationToken);

            // 4) ایجاد دیتابیس فیزیکی + migration.
            try
            {
                await _provisioner.ProvisionAsync(connectionString, cancellationToken);

                // 5) seed نقش‌ها و اولین ادمین داخل دیتابیس tenant جدید.
                await using var tenantDb = _tenantDbFactory.CreateForConnection(connectionString);
                await _seeder.SeedRolesAsync(tenantDb, cancellationToken);
                await _seeder.SeedFirstAdminAsync(
                    tenantDb, request.AdminFullName, request.AdminEmail, request.AdminPassword, cancellationToken);

                // 6) فعال‌سازی tenant.
                tenant.Status = TenantStatus.Active;
                await _catalog.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provisioning tenant {Slug} ناموفق بود", request.Slug);
                tenant.Status = TenantStatus.Suspended;
                await _catalog.SaveChangesAsync(cancellationToken);
                throw new AppException($"ساخت tenant ناموفق بود: {ex.Message}", "provisioning_failed");
            }
        }

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            ConnectionName = tenant.ConnectionName,
            AdminEmail = tenant.AdminEmail,
            Status = tenant.Status,
            CreatedAt = tenant.CreatedAt
        };
    }
}
