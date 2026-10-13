using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public interface IEventsDispatcher
{
    Task DispatchAsync();
}

public class DispatchingDecorator<T> : INotificationHandler<T>
    where T : INotification
{
    private readonly INotificationHandler<T> _decorated;
    private readonly IEventsDispatcher _dispatcher;

    public DispatchingDecorator(INotificationHandler<T> decorated, IEventsDispatcher dispatcher)
    {
        _decorated = decorated;
        _dispatcher = dispatcher;
    }

    public async Task Handle(T notification, CancellationToken cancellationToken)
    {
        await _decorated.Handle(notification, cancellationToken);
        await _dispatcher.DispatchAsync();
    }
}