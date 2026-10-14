using System;
using System.Collections.Generic;
using AutoMapper;

namespace Shopping;

public class ItemEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public class Item
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public class ItemProfile : Profile
{
    public ItemProfile()
    {
        CreateMap<ItemEntity, Item>()
            .ConvertUsing((src, _, ctx) => new Item
            {
                Id = src.Id,
                Name = ctx.Mapper.Map<string>(src.Name),
            });
    }
}
