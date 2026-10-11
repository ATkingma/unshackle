using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Placed(int Id) : INotification;

public class PlacedHandler : INotificationHandler<Placed>
{
    public ValueTask Handle(Placed notification, CancellationToken cancellationToken) =>
        new ValueTask(Task.Delay(notification.Id, cancellationToken));
}
