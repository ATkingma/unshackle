using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using SampleShop.Data;
using SampleShop.Dtos;

namespace SampleShop.Features.Orders;

public class ListOrdersHandler : IRequestHandler<ListOrdersQuery, List<OrderDto>>
{
    private readonly OrderRepository _repository;
    private readonly IMapper _mapper;

    public ListOrdersHandler(OrderRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    // ProjectTo: Unshackle should report this as manual work.
    public Task<List<OrderDto>> Handle(ListOrdersQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(_repository.Query().ProjectTo<OrderDto>(_mapper.ConfigurationProvider).ToList());
}
