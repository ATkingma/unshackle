using AutoMapper;
using MediatR;
using SampleShop.Data;
using SampleShop.Domain;

namespace SampleShop.Features.Orders;

public class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, int>
{
    private readonly OrderRepository _repository;
    private readonly IMapper _mapper;
    private readonly IPublisher _publisher;

    public PlaceOrderHandler(OrderRepository repository, IMapper mapper, IPublisher publisher)
    {
        _repository = repository;
        _mapper = mapper;
        _publisher = publisher;
    }

    public async Task<int> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Lines.Count == 0)
        {
            throw new ArgumentException("An order needs at least one line.", nameof(request));
        }

        var order = new Order
        {
            CustomerName = request.CustomerName,
            Lines = _mapper.Map<List<OrderLine>>(request.Lines),
        };
        var id = _repository.Add(order);

        await _publisher.Publish(new OrderPlaced(id), cancellationToken);
        return id;
    }
}
