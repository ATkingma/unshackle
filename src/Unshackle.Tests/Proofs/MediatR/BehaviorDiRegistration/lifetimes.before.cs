using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public class LoggingBehavior<TRequest, TResponse>;

public class ValidationBehavior<TRequest, TResponse>;

public record Ping : IRequest<int>;

public class PingBehavior;

public static class Registration
{
    public static IServiceCollection AddBehaviors(this IServiceCollection services) =>
        services
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))
            .AddSingleton(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>))
            .AddTransient(typeof(IPipelineBehavior<Ping, int>), typeof(PingBehavior));
}
