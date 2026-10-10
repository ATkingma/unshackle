using AutoMapper;
using SampleShop.Domain;
using SampleShop.Dtos;

namespace SampleShop.Mapping;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.LineCount, o => o.MapFrom(s => s.Lines.Count))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

        CreateMap<OrderLine, OrderLineDto>().ReverseMap();
    }
}
