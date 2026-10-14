using AutoMapper;

namespace Shop;

public class Address
{
    public string City { get; set; } = "";
}

public class AddressDto
{
    public string City { get; set; } = "";
}

public struct Point
{
    public int X { get; set; }
}

public struct PointDto
{
    public int X { get; set; }
}

public class Tag
{
    public string Value { get; set; } = "";
}

public struct TagDto
{
    public string Value { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public Address Address { get; set; } = new();
    public Point Point { get; set; }
    public Point? Corner { get; set; }
    public Tag Tag { get; set; } = new();
}

public class OrderDto
{
    public int Id { get; set; }
    public AddressDto Address { get; set; } = new();
    public PointDto Point { get; set; }
    public PointDto? Corner { get; set; }
    public TagDto Tag { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
    }
}

public static class OrderProfileExtensions
{
    public static OrderDto ToOrderDto(this Order source) => new()
    {
        Id = source.Id,
        Address = source.Address?.ToAddressDto(),
        Point = source.Point.ToPointDto(),
        Corner = source.Corner?.ToPointDto(),
        Tag = source.Tag?.ToTagDto() ?? default,
    };

    public static AddressDto ToAddressDto(this Address source) => new()
    {
        City = source.City,
    };

    public static PointDto ToPointDto(this Point source) => new()
    {
        X = source.X,
    };

    public static TagDto ToTagDto(this Tag source) => new()
    {
        Value = source.Value,
    };
}
