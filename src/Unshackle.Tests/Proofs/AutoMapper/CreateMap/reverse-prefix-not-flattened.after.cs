using System;
using AutoMapper;

namespace Shop;

// ShelfDto.WarehouseName starts with ShelfModel.Warehouse, but WarehouseModel has no Name: AutoMapper neither flattens it forward
// nor unflattens it in reverse (measured: Warehouse stays null, WarehouseName stays null).
public class WarehouseModel
{
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
}

public class ShelfModel
{
    public Guid ShelfId { get; set; }
    public Guid WarehouseId { get; set; }
    public WarehouseModel? Warehouse { get; set; }
}

public class ShelfDto
{
    public Guid ShelfId { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
}

public class ShelfMapping : Profile
{
    public ShelfMapping()
    {
    }
}

public static class ShelfMappingExtensions
{
    public static ShelfDto ToShelfDto(this ShelfModel source) => new()
    {
        ShelfId = source.ShelfId,
        WarehouseId = source.WarehouseId,
    };

    public static ShelfModel ToShelfModel(this ShelfDto source) => new()
    {
        ShelfId = source.ShelfId,
        WarehouseId = source.WarehouseId,
    };
}
