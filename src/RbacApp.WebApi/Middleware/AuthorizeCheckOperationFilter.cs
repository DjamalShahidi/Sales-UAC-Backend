using System.Reflection;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.SwaggerGen;
using OpenApiOperation = Microsoft.OpenApi.OpenApiOperation;
using OpenApiSecurityRequirement = Microsoft.OpenApi.OpenApiSecurityRequirement;
using OpenApiSecurityScheme = Microsoft.OpenApi.OpenApiSecurityScheme;
using OpenApiReference = Microsoft.OpenApi.OpenApiReference;

namespace RbacApp.WebApi.Middleware;

/// <summary>
/// Swagger operation filter for annotating required permissions and roles.
/// </summary>
public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var methodInfo = context.MethodInfo;

        var hasAllowAnonymous = methodInfo.GetCustomAttributes(true)
            .Any(m => m.GetType().Name == "AllowAnonymousAttribute");

        if (hasAllowAnonymous) return;

        var allPerms = GetPermissionValues(methodInfo);
        var allRoles = GetRoleValues(methodInfo);

        if (allPerms.Count == 0 && allRoles.Count == 0)
            allPerms.Add("authenticated");

        var desc = new List<string>();
        if (allPerms.Count > 0) desc.Add($"Permissions: {string.Join(", ", allPerms)}");
        if (allRoles.Count > 0) desc.Add($"Roles: {string.Join(", ", allRoles)}");

        if (desc.Count > 0)
            operation.Description = string.Join(" | ", desc);

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    }

    private static List<string> GetPermissionValues(MethodInfo method)
    {
        var result = new List<string>();

        foreach (var attr in method.GetCustomAttributes(true).Where(a => a.GetType().Name == "RequirePermissionAttribute"))
        {
            var prop = attr.GetType().GetProperty("Permissions");
            if (prop != null)
                result.AddRange((prop.GetValue(attr) as string[]) ?? Array.Empty<string>());
        }

        if (method.DeclaringType is not null)
        {
            foreach (var attr in method.DeclaringType.GetCustomAttributes(true)
                         .Where(a => a.GetType().Name == "RequirePermissionAttribute"))
            {
                var prop = attr.GetType().GetProperty("Permissions");
                if (prop != null)
                    result.AddRange((prop.GetValue(attr) as string[]) ?? Array.Empty<string>());
            }
        }

        return result.Distinct().ToList();
    }

    private static List<string> GetRoleValues(MethodInfo method)
    {
        var result = new List<string>();

        foreach (var attr in method.GetCustomAttributes(true).Where(a => a.GetType().Name == "RequireRoleAttribute"))
        {
            var prop = attr.GetType().GetProperty("Roles");
            if (prop != null)
                result.AddRange((prop.GetValue(attr) as string[]) ?? Array.Empty<string>());
        }

        if (method.DeclaringType is not null)
        {
            foreach (var attr in method.DeclaringType.GetCustomAttributes(true)
                         .Where(a => a.GetType().Name == "RequireRoleAttribute"))
            {
                var prop = attr.GetType().GetProperty("Roles");
                if (prop != null)
                    result.AddRange((prop.GetValue(attr) as string[]) ?? Array.Empty<string>());
            }
        }

        return result.Distinct().ToList();
    }
}
