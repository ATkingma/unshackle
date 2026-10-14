#nullable enable
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

public static class OrderMapping
{
    public static OrderDto ToOrderDto(this Order source) => new() { Id = source.Id };
}

public interface IOrderRepository
{
    Task<Order?> FindAsync(int id);
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

    public OrderDto? Find(Order? order) => _mapper.Map<OrderDto?>(order);

    public async Task<OrderDto?> GetAsync(int id)
    {
        var order = await _repository.FindAsync(id);
        return _mapper.Map<OrderDto>(order);
    }

    public int? Id(Order? order) => _mapper.Map<OrderDto?>(order)?.Id;
}
