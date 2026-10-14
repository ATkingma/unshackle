using AutoMapper;

namespace Shop;

public enum Priority { Low, High }

public enum PriorityDto { High, Low }

public class Order
{
    public Priority? Priority { get; set; }
}

public class OrderDto
{
    public PriorityDto Priority { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
