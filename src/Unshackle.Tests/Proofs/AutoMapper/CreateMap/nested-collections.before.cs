using System.Collections.Generic;
using AutoMapper;

namespace Shop;

public class Line
{
    public int Quantity { get; set; }
}

public class LineDto
{
    public int Quantity { get; set; }
}

public struct Point
{
    public int X { get; set; }
}

public struct PointDto
{
    public int X { get; set; }
}

public class Order
{
    public List<Line> Lines { get; set; } = [];
    public Line[] Archived { get; set; } = [];
    public IEnumerable<Line> Pending { get; set; } = [];
    public ICollection<Line> Returned { get; set; } = [];
    public IReadOnlyList<Line> Shipped { get; set; } = [];
    public List<Point> Corners { get; set; } = [];
    public List<string> Notes { get; set; } = [];
}

public class OrderDto
{
    public List<LineDto> Lines { get; set; } = [];
    public LineDto[] Archived { get; set; } = [];
    public IEnumerable<LineDto> Pending { get; set; } = [];
    public IList<LineDto> Returned { get; set; } = [];
    public IReadOnlyCollection<LineDto> Shipped { get; set; } = [];
    public List<PointDto> Corners { get; set; } = [];
    public string[] Notes { get; set; } = [];
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<Line, LineDto>();
        CreateMap<Point, PointDto>();
    }
}
