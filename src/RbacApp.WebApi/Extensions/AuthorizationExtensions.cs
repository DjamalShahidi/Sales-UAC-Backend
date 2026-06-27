using Microsoft.AspNetCore.Authorization;

namespace RbacApp.WebApi.Extensions;

/// <summary>
/// ثبت policy سفارشی مبتنی بر claim "permission".
/// </summary>
public static class AuthorizationExtensions
{
    public static AuthorizationOptions AddPermissionPolicies(this AuthorizationOptions options)
    {
        // Policy عمومی برای دسترسی احرازشده.
        options.AddPolicy("Authenticated", policy =>
            policy.RequireAuthenticatedUser());

        // Policy برای super admin.
        options.AddPolicy("SuperAdmin", policy =>
            policy.RequireClaim("role", "SuperAdmin"));

        return options;
    }
}
