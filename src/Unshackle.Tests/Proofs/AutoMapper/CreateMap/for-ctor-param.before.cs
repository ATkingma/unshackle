using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public record OrderDto(int Id, string Title);

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForCtorParam("Title", o => o.MapFrom(s => s.Name));
    }
}
