using MediatR.Pipeline;
using SampleShop.Data;

namespace SampleShop.Behaviors;

public class AuditPreProcessor<TRequest> : IRequestPreProcessor<TRequest> where TRequest : notnull
{
    private readonly AuditLog _log;

    public AuditPreProcessor(AuditLog log)
    {
        _log = log;
    }

    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        _log.Entries.Add($"Pre {typeof(TRequest).Name}");
        return Task.CompletedTask;
    }
}

public class AuditPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse> where TRequest : notnull
{
    private readonly AuditLog _log;

    public AuditPostProcessor(AuditLog log)
    {
        _log = log;
    }

    public Task Process(TRequest request, TResponse response, CancellationToken cancellationToken)
    {
        _log.Entries.Add($"Post {typeof(TRequest).Name}");
        return Task.CompletedTask;
    }
}
