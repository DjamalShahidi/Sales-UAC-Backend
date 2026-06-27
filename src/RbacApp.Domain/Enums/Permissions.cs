namespace RbacApp.Domain.Enums;

/// <summary>
/// فهرست ثابت تمام دسترسی‌های سیستم.
/// هر مقدار به‌صورت "module.action" نام‌گذاری شده تا در UI و claimها خوانا باشد.
/// مقادیر جدید فقط باید اضافه شوند — هرگز حذف یا جابجا نشوند.
/// </summary>
public static class Permissions
{
    // ----- Dashboard -----
    public const string DashboardView = "dashboard.view";

    // ----- Users -----
    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";
    public const string UsersManageRoles = "users.manage_roles";

    // ----- Roles -----
    public const string RolesView = "roles.view";
    public const string RolesCreate = "roles.create";
    public const string RolesUpdate = "roles.update";
    public const string RolesDelete = "roles.delete";
    public const string RolesManagePermissions = "roles.manage_permissions";

    // ----- Tenants (super admin only) -----
    public const string TenantsView = "tenants.view";
    public const string TenantsCreate = "tenants.create";
    public const string TenantsUpdate = "tenants.update";
    public const string TenantsDelete = "tenants.delete";

    /// <summary>
    /// تمام مقادیر دسترسی به‌عنوان لیست.
    /// </summary>
    public static readonly IReadOnlyCollection<string> All = new[]
    {
        DashboardView,

        UsersView, UsersCreate, UsersUpdate, UsersDelete, UsersManageRoles,

        RolesView, RolesCreate, RolesUpdate, RolesDelete, RolesManagePermissions,

        TenantsView, TenantsCreate, TenantsUpdate, TenantsDelete
    };

    /// <summary>
    /// دسته‌بندی دسترسی‌ها برای نمایش در UI (matrix).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> Groups =
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["dashboard"] = new[] { DashboardView },
            ["users"] = new[]
            {
                UsersView, UsersCreate, UsersUpdate, UsersDelete, UsersManageRoles
            },
            ["roles"] = new[]
            {
                RolesView, RolesCreate, RolesUpdate, RolesDelete, RolesManagePermissions
            },
            ["tenants"] = new[]
            {
                TenantsView, TenantsCreate, TenantsUpdate, TenantsDelete
            }
        };
}
