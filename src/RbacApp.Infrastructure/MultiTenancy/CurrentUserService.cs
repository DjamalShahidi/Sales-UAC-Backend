using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RbacApp.Application.Common.Interfaces;

namespace RbacApp.Infrastructure.MultiTenancy;

/// <summary>
/// استخراج اطلاعات کاربر احرازشده‌ی جاری از HttpContext.Claims.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor)
        => _accessor = accessor;

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirstValue("sub") ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirstValue("email") ?? User?.FindFirstValue(ClaimTypes.Email);

    public string? FullName => User?.FindFirstValue("fullName");

    public Guid? TenantId
    {
        get
        {
            var val = User?.FindFirstValue("tenantId");
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    public string? TenantSlug => User?.FindFirstValue("tenantSlug");

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsSuperAdmin => IsInRole("SuperAdmin");

    public IReadOnlyCollection<string> Roles
        => (IReadOnlyCollection<string>?)(User?.FindAll("role").Select(c => c.Value).ToList())
           ?? Array.Empty<string>();

    public IReadOnlyCollection<string> Permissions
        => (IReadOnlyCollection<string>?)(User?.FindAll("permission").Select(c => c.Value).ToList())
           ?? Array.Empty<string>();

    public bool HasPermission(string permission)
        => User?.HasClaim("permission", permission) ?? false;

    public bool IsInRole(string role)
        => User?.HasClaim("role", role) ?? false;
}
