using Microsoft.Extensions.DependencyInjection;
using Mediator;

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
        services.AddMediator(cfg =>
        {
            // handlers
            // logging
            cfg.ServiceLifetime = ServiceLifetime.Transient;
            cfg.PipelineBehaviors = [typeof(AuditPreProcessor<,>), typeof(LoggingBehavior<,>), typeof(AuditPostProcessor<,>)];
            cfg.StreamPipelineBehaviors = [typeof(StreamLoggingBehavior<,>)];
        });
    }
}
