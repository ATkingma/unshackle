using System;
using AutoMapper;

namespace Shop;

public class OrderForm
{
    public string OrderedAt { get; set; } = "";
}

public class Order
{
    public DateTime OrderedAt { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<OrderForm, Order>();
    }
}

public static class MappingSetup
{
    public static IMapper Create() =>
        new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<OrderProfile>();
            cfg.CreateMap<string, DateTime>().ConvertUsing(s => DateTime.ParseExact(s, "yyyyMMdd", null));
        }).CreateMapper();
}
