using System;
using System.Collections.Generic;
using AutoMapper;

namespace Shop;

// Category → CategoryDto is a real cycle (Children on both sides): AutoMapper keeps references, so it stays on AutoMapper.
// BoxModel.Order is a destination-only cycle: that map migrates.
public class Category
{
    public string Name { get; set; } = "";
    public List<Category> Children { get; set; } = [];
}

public class CategoryDto
{
    public string Name { get; set; } = "";
    public List<CategoryDto> Children { get; set; } = [];
}

public class OrderModel
{
    public Guid OrderId { get; set; }
    public List<BoxModel> Boxes { get; set; } = [];
}

public class BoxModel
{
    public Guid OrderId { get; set; }
    public OrderModel? Order { get; set; }
}

public class OrderDto
{
    public Guid OrderId { get; set; }
    public List<BoxDto> Boxes { get; set; } = [];
}

public class BoxDto
{
    public Guid OrderId { get; set; }
}

public class ShopMapping : Profile
{
    public ShopMapping()
    {
        CreateMap<Category, CategoryDto>();
        CreateMap<OrderDto, OrderModel>();
        CreateMap<BoxDto, BoxModel>();
    }
}

public class ShopService
{
    private readonly IMapper _mapper;

    public ShopService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public CategoryDto Tree(Category root) => _mapper.Map<CategoryDto>(root);

    public OrderModel Add(OrderDto order) => _mapper.Map<OrderModel>(order);
}
