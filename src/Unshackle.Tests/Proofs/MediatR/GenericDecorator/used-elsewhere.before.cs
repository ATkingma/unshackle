using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public class Ping : IRequest
{
}

public class LoggingDecorator<T> : IRequestHandler<T>
    where T : IRequest
{
    private readonly IRequestHandler<T> _decorated;

    public LoggingDecorator(IRequestHandler<T> decorated)
    {
        _decorated = decorated;
    }

    public Task Handle(T request, CancellationToken cancellationToken) => _decorated.Handle(request, cancellationToken);
}

public class Factory
{
    public IRequestHandler<Ping> Wrap(IRequestHandler<Ping> inner) => new LoggingDecorator<Ping>(inner);
}