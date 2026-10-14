#nullable enable
using System.Collections.Generic;
using System.Linq;
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
    }
}

public static class OrderProfileExtensions
{
    public static OrderDto ToOrderDto(this Order source) => new()
    {
        Id = source.Id,
    };
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
        return orders.Select(x => x.ToOrderDto()).ToList();
    }

    public List<OrderDto> Maybe(List<Order>? orders) => orders?.Select(x => x.ToOrderDto()).ToList() ?? [];
}
