using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}

public class OrderService
{
    private readonly IMapper _mapper;

    public OrderService(IMapper mapper)
    {
        _mapper = mapper;
    }

    // Map(source, destination) as a method group: not rewritten, so the CreateMap must stay too
    public List<OrderDto> Copy(IEnumerable<Order> orders, OrderDto[] targets) => orders.Zip(targets, _mapper.Map).ToList();
}