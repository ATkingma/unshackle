using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public class LoggingDecorator<T> : IRequestHandler<T>
    where T : IRequest
{
    private readonly IRequestHandler<T> _decorated;

    public LoggingDecorator(IRequestHandler<T> decorated)
    {
        _decorated = decorated;
    }

    public Task Handle(T request, CancellationToken cancellationToken)
    {
        Console.WriteLine("Handled by " + _decorated.GetType().Name);
        return _decorated.Handle(request, cancellationToken);
    }
}