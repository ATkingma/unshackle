using AutoMapper;

namespace Shop;

public struct Money
{
    public decimal Amount { get; set; }

    public static implicit operator decimal(Money money) => money.Amount;
}

public class Order
{
    public Money Total { get; set; }
}

public class OrderDto
{
    public decimal Total { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
