using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Collections.Generic;

namespace RbacApp.WebApi.Middleware;

/// <summary>
/// Swagger operation filter for annotating required permissions.
/// </summary>
public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var allowAnonymous = context.MethodInfo
            .GetCustomAttributes(true)
            .Any(m => m is AllowAnonymousAttribute);

        if (allowAnonymous) return;

        var requirePermissionAttrs = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<RequirePermissionAttribute>()
            .ToList();

        var requireRoleAttrs = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<RequireRoleAttribute>()
            .ToList();

        var classPermAttrs = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(true)
            .OfType<RequirePermissionAttribute>()
            .ToList() ?? new();

        var classRoleAttrs = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(true)
            .OfType<RequireRoleAttribute>()
            .ToList() ?? new();

        var allPerms = requirePermissionAttrs.Union(classPermAttrs)
            .SelectMany(a => a.GetType().GetProperty("Permissions")?.GetValue(a) as string[] ?? Array.Empty<string>())
            .Distinct().ToList();

        var allRoles = requireRoleAttrs.Union(classRoleAttrs)
            .SelectMany(a => a.GetType().GetProperty("Roles")?.GetValue(a) as string[] ?? Array.Empty<string>())
            .Distinct().ToList();

        if (allPerms.Count == 0 && allRoles.Count == 0)
            allPerms.Add("authenticated");

        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            }
        };

        var tags = new List<string>();
        if (allPerms.Count > 0) tags.Add($"Permissions: {string.Join(", ", allPerms)}");
        if (allRoles.Count > 0) tags.Add($"Roles: {string.Join(", ", allRoles)}");

        if (tags.Count > 0 && operation.Description is null)
            operation.Description = string.Join(" | ", tags);
    }
}
