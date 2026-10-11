using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public class LoggingBehavior<TRequest, TResponse>;

public static class Registration
{
    public static IServiceCollection AddShop(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Registration).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        });
        return services;
    }
}
