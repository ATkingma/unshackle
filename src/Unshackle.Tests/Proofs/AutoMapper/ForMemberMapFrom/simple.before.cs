using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
    public int Price { get; set; }
    public int Quantity { get; set; }
    public string Number { get; set; } = "";
}

public class OrderDto
{
    public int Id { get; set; }
    public long Total { get; set; }
    public string Reference { get; set; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Total, o => o.MapFrom(s => s.Price * s.Quantity))
            .ForMember(d => d.Reference, o => o.MapFrom(s => "#" + s.Number));
    }
}
