using System.Collections.Generic;
using AutoMapper;

namespace Shop;

public class Customer
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public Customer Customer { get; set; } = new();
    public List<int> Numbers { get; set; } = [];
}

public class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public string Text { get; set; } = "";
    public int CustomerAge { get; set; }
    public List<int> Numbers { get; set; } = [];
}

public class OrderProfile : Profile
{
    private readonly string _prefix = "#";

    public OrderProfile()
    {
    }
}

public static class OrderProfileExtensions
{
    public static OrderDto ToOrderDto(this Order source) => new()
    {
        Id = source.Id,
        CustomerName = source.Customer?.Name,
        CustomerAge = source.Customer?.Age ?? default,
    };
}
