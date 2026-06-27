using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RbacApp.Application.Common.Behaviors;

namespace RbacApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // اعتبارسنجی خودکار همه‌ی AbstractValidatorها.
        services.AddValidatorsFromAssembly(assembly);

        // Behaviorها — به ترتیب ثبت مهم است.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

        return services;
    }
}
