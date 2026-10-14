#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}

public interface IOrderRepository
{
    Task<List<Order>> ListAsync();
}

public class OrderService
{
    private readonly IMapper _mapper;
    private readonly IOrderRepository _repository;

    public OrderService(IMapper mapper, IOrderRepository repository)
    {
        _mapper = mapper;
        _repository = repository;
    }

    public async Task<ICollection<OrderDto>> AllAsync()
    {
        var orders = await _repository.ListAsync();
        return _mapper.Map<List<OrderDto>>(orders);
    }

    public List<OrderDto> Maybe(List<Order>? orders) => _mapper.Map<List<OrderDto>>(orders);
}
