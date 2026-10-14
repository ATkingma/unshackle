using AutoMapper;

namespace Shop;

public class Order
{
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal? Discount { get; set; }
    public int First { get; set; }
    public int Second { get; set; }
}

public class OrderDto
{
    public int Total { get; set; }
    public decimal Rebate { get; set; }
    public string Label { get; set; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Total, o => o.MapFrom(s => s.Price * s.Quantity))
            .ForMember(d => d.Rebate, o => o.MapFrom(s => s.Discount))
            .ForMember(d => d.Label, o => o.MapFrom(s => s.First + s.Second));
    }
}
