using System;
using System.Collections.Generic;
using AutoMapper;

namespace Shop;

// BoxModel.Order closes a type cycle OrderModel → BoxModel → OrderModel, but BoxDto has no Order: AutoMapper never follows it.
public class OrderModel
{
    public Guid OrderId { get; set; }
    public ICollection<BoxModel> Boxes { get; set; } = new List<BoxModel>();
}

public class BoxModel
{
    public Guid OrderId { get; set; }
    public OrderModel? Order { get; set; }
    public string Type { get; set; } = "";
}

public class OrderDto
{
    public Guid OrderId { get; set; }
    public List<BoxDto> Boxes { get; set; } = [];
}

public class BoxDto
{
    public Guid OrderId { get; set; }
    public string Type { get; set; } = "";
}

public class OrderMapping : Profile
{
    public OrderMapping()
    {
        CreateMap<OrderDto, OrderModel>().ReverseMap();
        CreateMap<BoxModel, BoxDto>().ReverseMap();
    }
}

public class OrderService
{
    private readonly IMapper _mapper;

    public OrderService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public OrderModel Add(OrderDto order) => _mapper.Map<OrderModel>(order);

    public OrderDto Get(OrderModel order) => _mapper.Map<OrderDto>(order);
}
