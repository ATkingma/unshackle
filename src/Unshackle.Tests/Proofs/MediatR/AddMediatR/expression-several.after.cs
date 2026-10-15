using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public class LoggingBehavior<TRequest, TResponse>;

public static class Registration
{
    public static IServiceCollection AddShop(this IServiceCollection services)
    {
        services.AddMediator(cfg =>
        {
            cfg.ServiceLifetime = ServiceLifetime.Transient;
            cfg.PipelineBehaviors = [typeof(LoggingBehavior<,>)];
        });
        return services;
    }
}
