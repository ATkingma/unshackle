using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public class LoggingBehavior<TRequest, TResponse>;

public class ValidationBehavior;

public class AuditBehavior;

public static class Registration
{
    public static IServiceCollection AddShop(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<ValidationBehavior>();
            cfg.Lifetime = ServiceLifetime.Scoped;
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddBehavior<object, ValidationBehavior>();
            cfg.AddBehavior(typeof(AuditBehavior));
        });
        return services;
    }
}
