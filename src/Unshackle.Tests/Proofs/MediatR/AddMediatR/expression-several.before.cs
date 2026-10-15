using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public class LoggingBehavior<TRequest, TResponse>;

public static class Registration
{
    public static IServiceCollection AddShop(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.AddOpenBehavior(typeof(LoggingBehavior<,>)));
        return services;
    }
}
