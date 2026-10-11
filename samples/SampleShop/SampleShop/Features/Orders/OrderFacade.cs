using MediatR;
using SampleShop.Domain;
using SampleShop.Features.Common;
using SampleShop.Legacy;

namespace SampleShop.Features.Orders;

// Injects ISender and IMediator (not only IPublisher), as application services often do.
public class OrderFacade
{
    private readonly ISender _sender;
    private readonly IMediator _mediator;

    public OrderFacade(ISender sender, IMediator mediator)
    {
        _sender = sender;
        _mediator = mediator;
    }

    public Task CancelAsync(int id, CancellationToken cancellationToken) => _sender.Send(new CancelOrderCommand(id), cancellationToken);

    public Task<int> CountOrdersAsync(CancellationToken cancellationToken) => _mediator.Send(new CountQuery<Order>(), cancellationToken);

#pragma warning disable CS0618 // deliberate caller of the obsolete API (roadmap step 6)
    public decimal PriceWithVat(decimal amount) => PriceCalculator.AddVat(amount);
#pragma warning restore CS0618
}
