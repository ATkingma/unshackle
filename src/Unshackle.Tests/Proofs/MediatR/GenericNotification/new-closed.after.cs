using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record OrderPlaced(int Id);

public record OrderShipped(int Id);

public abstract class Envelope<TEvent> : INotification
{
    public Envelope(TEvent payload, string source = "shop")
    {
        Payload = payload;
        Source = source;
    }

    public TEvent Payload { get; }

    public string Source { get; }
}

public class EnvelopeOfOrderPlaced : Envelope<OrderPlaced>
{
    public EnvelopeOfOrderPlaced(OrderPlaced payload, string source = "shop")
        : base(payload, source)
    {
    }
}

public class EnvelopeOfOrderShipped : Envelope<OrderShipped>
{
    public EnvelopeOfOrderShipped(OrderShipped payload, string source = "shop")
        : base(payload, source)
    {
    }
}

public class Shop
{
    private readonly IPublisher _publisher;

    public Shop(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PlaceAsync(int id)
    {
        await _publisher.Publish(new EnvelopeOfOrderPlaced(new OrderPlaced(id)));
        Envelope<OrderShipped> shipped = new EnvelopeOfOrderShipped(new OrderShipped(id), "warehouse");
        await _publisher.Publish(shipped);
    }
}