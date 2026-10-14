using AutoMapper;

namespace Shop;

public class Order
{
    public long Id { get; set; }
    public double Weight { get; set; }
    public double Ratio { get; set; }
    public int Count { get; set; }
    public float Score { get; set; }
    public int? Lines { get; set; }
    public long? Version { get; set; }
    public bool Paid { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public int Weight { get; set; }
    public float Ratio { get; set; }
    public long Count { get; set; }
    public double Score { get; set; }
    public long Lines { get; set; }
    public int Version { get; set; }
    public int Paid { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
