namespace RbacApp.Application.Dtos;

/// <summary>بدنه‌ی درخواست ورود.</summary>
public record LoginRequest
{
    public string TenantSlug { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

/// <summary>بدنه‌ی تمدید توکن.</summary>
public record RefreshTokenRequest
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
}

/// <summary>ثبت اولین ادمین tenant (فقط وقتی tenant خالی باشد).</summary>
public record RegisterFirstAdminRequest
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

/// <summary>بدنه‌ی ساخت tenant جدید (super admin).</summary>
public record CreateTenantRequest
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string ConnectionName { get; init; } = string.Empty;

    /// <summary>اطلاعات ادمین اولیه‌ی tenant جدید.</summary>
    public FirstAdminDto Admin { get; init; } = new();
}

public record FirstAdminDto
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
