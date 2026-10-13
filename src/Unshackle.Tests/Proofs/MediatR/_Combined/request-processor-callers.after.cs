using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record CreateItem(string Title) : IRequest<int>;

public record Ping : IRequest;

public class LoggingBehaviour<TRequest, TResponse> : MessagePreProcessor<TRequest, TResponse> where TRequest : notnull, IMessage
{
    private readonly ILogSink _sink;

    public LoggingBehaviour(ILogSink sink)
    {
        _sink = sink;
    }

    protected override ValueTask Handle(TRequest request, CancellationToken cancellationToken)
    {
        _sink.Write(typeof(TRequest).Name);
        return ValueTask.CompletedTask;
    }
}

public class AuditPostProcessor : MessagePostProcessor<CreateItem, int>
{
    protected override ValueTask Handle(CreateItem request, int response, CancellationToken cancellationToken) => ValueTask.CompletedTask;
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
        var requestLogger = new LoggingBehaviour<CreateItem, int>(_sink);

        await requestLogger.Handle(new CreateItem("a"), static (_, _) => default, new CancellationToken());
    }

    public async Task Process_Unit()
    {
        LoggingBehaviour<Ping, Unit> logger = new(_sink);

        await logger.Handle(new Ping(), static (_, _) => default, CancellationToken.None);
    }

    public async Task Post_Audits()
    {
        var audit = new AuditPostProcessor();

        await audit.Handle(new CreateItem("b"), (_, _) => new ValueTask<int>(5), CancellationToken.None);
    }

    private class NullSink : ILogSink
    {
        public void Write(string text)
        {
        }
    }
}
