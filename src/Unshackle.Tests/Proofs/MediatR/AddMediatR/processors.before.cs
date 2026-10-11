using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public class LoggingBehavior<TRequest, TResponse>;

public class StreamLoggingBehavior<TRequest, TResponse>;

public class AuditPreProcessor<TRequest>;

// What RequestProcessorPattern makes of AuditPreProcessor<TRequest> in the same run.
public class AuditPreProcessor<TRequest, TResponse>;

public class AuditPostProcessor<TRequest, TResponse>;

public static class Registration
{
    public static void AddShop(IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            // handlers
            cfg.RegisterServicesFromAssembly(typeof(Registration).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>)); // logging
            cfg.AddOpenStreamBehavior(typeof(StreamLoggingBehavior<,>));
            cfg.AddOpenRequestPreProcessor(typeof(AuditPreProcessor<>));
            cfg.AddOpenRequestPostProcessor(typeof(AuditPostProcessor<,>));
        });
    }
}
