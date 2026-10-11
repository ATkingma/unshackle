using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public class LoggingBehavior<TRequest, TResponse>;

public static class Registration
{
    public static IServiceCollection AddBehaviors(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        return services;
    }
}
