using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public string Note { get; set; } = "";
    public int Rank { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>().ForMember(d => d.Rank, o => o.Ignore()).ForMember("Note", o => o.Ignore());
    }
}
