using AutoMapper;

namespace Shop;

// The forward map flattens Customer.Name into CustomerName, so the reverse map unflattens it (measured: it creates the Customer).
public class Customer
{
    public string Name { get; set; } = "";
}

public class Order
{
    public Customer? Customer { get; set; }
}

public class OrderDto
{
    public string CustomerName { get; set; } = "";
}

public class OrderMapping : Profile
{
    public OrderMapping()
    {
        CreateMap<Order, OrderDto>().ReverseMap();
    }
}
