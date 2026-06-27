namespace RbacApp.Domain.Exceptions;

/// <summary>
/// استثنای پایه‌ی برنامه. به HTTP 400 تبدیل می‌شود.
/// </summary>
public class AppException : Exception
{
    public string ErrorCode { get; }

    public AppException(string message, string errorCode = "app_error")
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public AppException(string message, Exception inner, string errorCode = "app_error")
        : base(message, inner)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>منبع پیدا نشد — HTTP 404.</summary>
public class NotFoundException : AppException
{
    public NotFoundException(string entity, object key)
        : base($"{entity} با شناسه '{key}' پیدا نشد.", "not_found") { }
}

/// <summary>قانون کسب‌وکار نقض شده — HTTP 409.</summary>
public class ConflictException : AppException
{
    public ConflictException(string message) : base(message, "conflict") { }
}

/// <summary>دسترسی به tenant ممنوع — HTTP 403.</summary>
public class TenantForbiddenException : AppException
{
    public TenantForbiddenException(string slug)
        : base($"دسترسی به tenant '{slug}' ممنوع است یا وجود ندارد.", "tenant_forbidden") { }
}

/// <summary>tenant پیدا نشد — HTTP 400.</summary>
public class TenantNotFoundException : AppException
{
    public TenantNotFoundException(string slug)
        : base($"tenant با slug '{slug}' پیدا نشد.", "tenant_not_found") { }
}

/// <summary>احراز هویت ناموفق — HTTP 401.</summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "احراز هویت ناموفق بود.")
        : base(message, "unauthorized") { }
}
