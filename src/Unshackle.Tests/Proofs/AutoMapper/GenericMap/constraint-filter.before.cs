using AutoMapper;

namespace Shop;

public interface IDto
{
}

public class Order
{
    public int Id { get; set; }
}

public class OrderDto : IDto
{
    public int Id { get; set; }
}

public static class OrderMapping
{
    public static OrderDto ToOrderDto(this Order source) => new() { Id = source.Id };
}

internal class Mapping
{
    private readonly IMapper _mapper;

    public Mapping(IMapper mapper)
    {
        _mapper = mapper;
    }

    public TDto To<TDto>(Order order)
        where TDto : class, IDto => _mapper.Map<TDto>(order);

    public TDto Through<TDto>(Order order)
        where TDto : class, IDto => To<TDto>(order);
}

internal class Caller
{
    private readonly Mapping _mapping;

    public Caller(Mapping mapping)
    {
        _mapping = mapping;
    }

    public OrderDto Get(Order order) => _mapping.Through<OrderDto>(order);
}
