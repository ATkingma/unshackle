using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public class LoggingBehavior<TRequest, TResponse>;

public static class Registration
{
    public static void AddShop(IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Registration).Assembly);
            cfg.AddOpenBehaviors([typeof(LoggingBehavior<,>)]);
        });
    }
}
