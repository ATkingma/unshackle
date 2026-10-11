namespace Proofs.Mediator
{
    public static class MediatorModule
    {
    }
}

namespace Proofs.Events
{
    using System.Threading;
    using System.Threading.Tasks;
    using global::Mediator;

    public record OrderPlaced(int Id) : INotification;

    public class OrderPlacedHandler : INotificationHandler<OrderPlaced>
    {
        public ValueTask Handle(OrderPlaced notification, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }
}
