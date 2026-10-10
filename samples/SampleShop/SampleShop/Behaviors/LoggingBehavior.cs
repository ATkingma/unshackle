using MediatR;
using SampleShop.Data;

namespace SampleShop.Behaviors;

// Pipeline behavior: Unshackle should report this as manual work.
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly AuditLog _log;

    public LoggingBehavior(AuditLog log)
    {
        _log = log;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _log.Entries.Add($"Handling {typeof(TRequest).Name}");
        return await next();
    }
}
