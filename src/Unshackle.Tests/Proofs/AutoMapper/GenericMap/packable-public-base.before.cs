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

public abstract class ReaderBase<TDto>
{
    private readonly IMapper _mapper;

    protected ReaderBase(IMapper mapper)
    {
        _mapper = mapper;
    }

    public TDto Read(Order order) => _mapper.Map<TDto>(order);
}

public class OrderReader : ReaderBase<OrderDto>
{
    public OrderReader(IMapper mapper)
        : base(mapper)
    {
    }
}
