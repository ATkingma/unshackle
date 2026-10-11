using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public interface IAppEvent : INotification;

public interface IAppEventHandler<in TEvent> : INotificationHandler<TEvent>
    where TEvent : IAppEvent;

public record OrderPlaced(int Id) : IAppEvent;

public class OrderPlacedHandler : IAppEventHandler<OrderPlaced>
{
    public async Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        await Task.Delay(notification.Id, cancellationToken);
    }
}
