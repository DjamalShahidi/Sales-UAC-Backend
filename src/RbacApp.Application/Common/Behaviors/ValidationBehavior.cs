using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace RbacApp.Application.Common.Behaviors;

/// <summary>
/// قبل از اجرای هر request، اعتبارسنجی‌های FluentValidation را اجرا می‌کند
/// و در صورت خطا، ValidationException پرتاب می‌کند.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
        {
            _logger.LogWarning("Validation failed for {Request}: {Errors}",
                typeof(TRequest).Name, failures);
            throw new ValidationException(failures);
        }

        return await next();
    }
}
