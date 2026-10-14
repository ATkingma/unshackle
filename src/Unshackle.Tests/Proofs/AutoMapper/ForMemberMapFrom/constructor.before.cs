using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public record OrderDto(int Id, string Name);

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name.ToUpperInvariant()));
    }
}
