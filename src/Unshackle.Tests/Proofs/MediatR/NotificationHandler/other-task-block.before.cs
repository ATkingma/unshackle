using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Placed(int Id) : INotification;

public class PlacedHandler : INotificationHandler<Placed>
{
    public Task Handle(Placed notification, CancellationToken cancellationToken)
    {
        if (notification.Id == 0)
        {
            return Task.CompletedTask;
        }

        return Task.Delay(notification.Id, cancellationToken);
    }
}
