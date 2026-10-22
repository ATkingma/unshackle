using System;
using System.Threading.Tasks;
using MassTransit;

public record OrderSubmitted(Guid OrderId);

// Council round 11, T6: a Publish that resolves to MassTransit's IPublishEndpoint is an Info row.
public class Orders
{
    private readonly IPublishEndpoint _publish;

    public Orders(IPublishEndpoint publish)
    {
        _publish = publish;
    }

    public Task Submit(OrderSubmitted order) => _publish.Publish(order);
}
