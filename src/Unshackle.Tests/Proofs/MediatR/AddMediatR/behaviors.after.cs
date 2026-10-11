using Microsoft.Extensions.DependencyInjection;
using Mediator;

namespace Proofs;

public class LoggingBehavior<TRequest, TResponse>;

public class ValidationBehavior;

public class AuditBehavior;

public static class Registration
{
    public static IServiceCollection AddShop(this IServiceCollection services)
    {
        services.AddMediator(cfg =>
        {
            cfg.ServiceLifetime = ServiceLifetime.Scoped;
            cfg.PipelineBehaviors = [typeof(LoggingBehavior<,>), typeof(ValidationBehavior), typeof(AuditBehavior)];
        });
        return services;
    }
}
