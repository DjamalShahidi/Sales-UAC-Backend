using AutoMapper;
using RbacApp.Application.Dtos;
using RbacApp.Domain.Entities;
using RbacApp.Domain.Entities.Identity;

namespace RbacApp.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Tenants
        CreateMap<Tenant, TenantDto>();
        CreateMap<Tenant, TenantSummaryDto>();

        // Users — نقش‌ها به‌صورت دستی در handler پر می‌شوند.
        CreateMap<ApplicationUser, UserDto>()
            .ForMember(d => d.Roles, opt => opt.Ignore())
            .ForMember(d => d.IsLockedOut,
                opt => opt.MapFrom(s => s.LockoutEnd.HasValue && s.LockoutEnd > DateTimeOffset.UtcNow));

        CreateMap<ApplicationUser, UserListDto>()
            .ForMember(d => d.Roles, opt => opt.Ignore());
    }
}
