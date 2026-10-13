using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record OrderPlaced(int Id);

public record OrderShipped(int Id);

public sealed class Envelope<TEvent> : INotification
{
    public Envelope(TEvent payload, string source = "shop")
    {
        Payload = payload;
        Source = source;
    }

    public TEvent Payload { get; }

    public string Source { get; }
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
        await _publisher.Publish(new Envelope<OrderPlaced>(new OrderPlaced(id)));
        Envelope<OrderShipped> shipped = new(new OrderShipped(id), "warehouse");
        await _publisher.Publish(shipped);
    }
}