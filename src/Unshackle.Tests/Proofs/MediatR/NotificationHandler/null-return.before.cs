using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Placed(int Id) : INotification;

public class PlacedHandler : INotificationHandler<Placed>
{
    public Task Handle(Placed notification, CancellationToken cancellationToken) => null!;
}
