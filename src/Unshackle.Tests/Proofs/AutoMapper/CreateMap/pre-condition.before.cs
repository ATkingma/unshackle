using System;
using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
    public bool Paid { get; set; }
    public decimal Amount { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public decimal PaidAmount { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.PaidAmount, o =>
            {
                o.PreCondition(s => s.Paid);
                o.MapFrom(s => s.Amount);
            });
    }
}
