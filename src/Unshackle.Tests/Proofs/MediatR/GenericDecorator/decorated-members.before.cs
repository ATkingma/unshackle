using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : IRequest
{
    string Name { get; }
}

public class LoggingDecorator<T> : ICommandHandler<T>
    where T : IRequest
{
    private readonly ICommandHandler<T> _decorated;

    public LoggingDecorator(ICommandHandler<T> decorated)
    {
        _decorated = decorated;
    }

    public string Name => _decorated.Name;

    public Task Handle(T request, CancellationToken cancellationToken) => _decorated.Handle(request, cancellationToken);
}