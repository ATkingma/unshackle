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

public class Customer
{
    public string Name { get; set; } = "";
}

public class CustomerDto
{
    public string Name { get; set; } = "";
}

public static class ShopMapping
{
    public static OrderDto ToOrderDto(this Order source) => new() { Id = source.Id };

    public static CustomerDto ToCustomerDto(this Customer source) => new() { Name = source.Name };
}

internal abstract class RepositoryBase<TEntity, TDto>
{
    private readonly IMapper _mapper;

    protected RepositoryBase(IMapper mapper)
    {
        _mapper = mapper;
    }

    public TDto ToDto(TEntity entity) => typeof(TEntity) == typeof(Order) && typeof(TDto) == typeof(OrderDto) ? (TDto)(object)((Order)(object)entity).ToOrderDto()
        : typeof(TEntity) == typeof(Customer) && typeof(TDto) == typeof(CustomerDto) ? (TDto)(object)((Customer)(object)entity).ToCustomerDto()
        : throw new global::System.InvalidOperationException($"No generated map for {typeof(TEntity)}, {typeof(TDto)}.");
}

internal class OrderRepository : RepositoryBase<Order, OrderDto>
{
    public OrderRepository(IMapper mapper)
        : base(mapper)
    {
    }
}

internal class CustomerRepository : RepositoryBase<Customer, CustomerDto>
{
    public CustomerRepository(IMapper mapper)
        : base(mapper)
    {
    }
}
