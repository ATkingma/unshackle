using AutoMapper;
using MediatR;
using SampleShop.Data;
using SampleShop.Dtos;

namespace SampleShop.Features.Orders;

public class GetOrderHandler : IRequestHandler<GetOrderQuery, OrderDto?>
{
    private readonly OrderRepository _repository;
    private readonly IMapper _mapper;

    public GetOrderHandler(OrderRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public Task<OrderDto?> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = _repository.Find(request.Id);
        return Task.FromResult(order is null ? null : _mapper.Map<OrderDto>(order));
    }
}
