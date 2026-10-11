using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Placed(int Id) : INotification;

public class PlacedHandler : INotificationHandler<Placed>
{
    public async ValueTask Handle(Placed notification, CancellationToken cancellationToken)
    {
        await Task.Delay(notification.Id, cancellationToken);
    }
}
