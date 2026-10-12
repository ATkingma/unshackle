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
        CreateMap<Order, OrderDto>();
        CreateMap<Line, LineDto>().ForMember(d => d.Total, o => o.Ignore());
    }
}
