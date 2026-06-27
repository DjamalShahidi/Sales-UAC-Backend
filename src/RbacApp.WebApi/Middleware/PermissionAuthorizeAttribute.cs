using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RbacApp.WebApi.Middleware;

/// <summary>
/// Attribute for permission-based access control using JWT claims.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _permissions;

    public RequirePermissionAttribute(params string[] permissions)
        => _permissions = permissions;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata
            .Any(m => m is Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute);
        if (allowAnonymous) return;

        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (user.HasClaim("role", "SuperAdmin")) return;

        var hasPermission = _permissions.Any(p =>
            user.HasClaim("permission", p));

        if (!hasPermission)
        {
            context.Result = new StatusCodeResult(403);
        }
    }
}

/// <summary>
/// Attribute for role-based access control using JWT claims.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public RequireRoleAttribute(params string[] roles)
        => _roles = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata
            .Any(m => m is Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute);
        if (allowAnonymous) return;

        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (user.HasClaim("role", "SuperAdmin")) return;

        var hasRole = _roles.Any(r =>
            user.HasClaim("role", r));

        if (!hasRole)
        {
            context.Result = new StatusCodeResult(403);
        }
    }
}
