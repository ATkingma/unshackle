using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediatR.Pipeline;

namespace Proofs;

public record CreateItem(string Title) : IRequest<int>;

public record Ping : IRequest;

public class LoggingBehaviour<TRequest> : IRequestPreProcessor<TRequest> where TRequest : notnull
{
    private readonly ILogSink _sink;

    public LoggingBehaviour(ILogSink sink)
    {
        _sink = sink;
    }

    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        _sink.Write(typeof(TRequest).Name);
        return Task.CompletedTask;
    }
}

public class AuditPostProcessor : IRequestPostProcessor<CreateItem, int>
{
    public Task Process(CreateItem request, int response, CancellationToken cancellationToken) => Task.CompletedTask;
}

public interface ILogSink
{
    void Write(string text);
}

public class LoggingBehaviourTests
{
    private readonly ILogSink _sink = new NullSink();

    public async Task Process_LogsName()
    {
        var requestLogger = new LoggingBehaviour<CreateItem>(_sink);

        await requestLogger.Process(new CreateItem("a"), new CancellationToken());
    }

    public async Task Process_Unit()
    {
        LoggingBehaviour<Ping> logger = new(_sink);

        await logger.Process(new Ping(), CancellationToken.None);
    }

    public async Task Post_Audits()
    {
        var audit = new AuditPostProcessor();

        await audit.Process(new CreateItem("b"), 5, CancellationToken.None);
    }

    private class NullSink : ILogSink
    {
        public void Write(string text)
        {
        }
    }
}
