using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace Shop;

public class Line
{
    public string Sku { get; set; } = "";
}

public class LineDto
{
    public string Sku { get; set; } = "";
}

public class Order
{
    public int Id { get; set; }
    public string Customer { get; set; } = "";
    public List<Line> Lines { get; set; } = [];
    public string Note { get; set; } = "";
    public string Status { get; set; } = "";
}

public class OrderDto
{
    public OrderDto(int id, string customer, List<LineDto> lines)
    {
        Id = id;
        Customer = customer;
        Lines = lines;
    }

    public int Id { get; }
    public string Customer { get; set; }
    public List<LineDto> Lines { get; }
    public string Note { get; init; } = "";
    public string Status { get; set; } = "new";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
    }
}

public static class OrderProfileExtensions
{
    public static LineDto ToLineDto(this Line source) => new()
    {
        Sku = source.Sku,
    };

    public static OrderDto ToOrderDto(this Order source) => new(source.Id, source.Customer, source.Lines?.Select(x => x?.ToLineDto()).ToList() ?? [])
    {
        Note = source.Note,
    };
}
