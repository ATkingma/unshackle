#nullable enable
using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
    public string Note { get; set; } = "";
}

public class OrderDto
{
    public int Id { get; set; }
    public string Note { get; set; } = "";
    public int Rank { get; set; }
    public string Label { get; set; } = "";
}

public class Line
{
    public int Quantity { get; set; }
}

public class LineDto
{
    public int Quantity { get; set; }
    public decimal Total { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Note, o => o.Ignore())
            .ForMember(d => d.Rank, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Label, o => o.MapFrom(s => s.Note.ToUpper()));
        CreateMap<Line, LineDto>().ForMember(d => d.Total, o => o.Ignore());
    }
}
