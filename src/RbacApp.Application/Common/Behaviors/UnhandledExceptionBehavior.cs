using MediatR;
using Microsoft.Extensions.Logging;

namespace RbacApp.Application.Common.Behaviors;

/// <summary>
/// رفتار سراسری برای ثبت استثناهای غیرمنتظره‌ی لایه‌ی Application.
/// </summary>
public class UnhandledExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> _logger;

    public UnhandledExceptionBehavior(ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Domain.Exceptions.AppException)
        {
            // استثناهای دامنه‌ای برنامه‌ریزی‌شده‌اند — دوباره پرتاب می‌شوند تا middleware مناسب هندل کند.
            throw;
        }
        catch (Exception ex)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogError(ex, "Unhandled exception for request {Name} ({@Request})", requestName, request);
            throw;
        }
    }
}
