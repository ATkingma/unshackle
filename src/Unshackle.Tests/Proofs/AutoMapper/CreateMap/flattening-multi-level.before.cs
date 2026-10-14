using AutoMapper;

namespace Shop;

public struct Point
{
    public int X { get; set; }
}

public class Address
{
    public string City { get; set; } = "";
    public Point Location { get; set; }
}

public class Customer
{
    public Address Address { get; set; } = new();
    public int Age { get; set; }

    public Customer GetParent() => this;
}

public class Order
{
    public Customer Customer { get; set; } = new();
    public Point Point { get; set; }
}

public class OrderDto
{
    public string CustomerAddressCity { get; set; } = "";
    public int CustomerAddressLocationX { get; set; }
    public int PointX { get; set; }
    public string CustomerAge { get; set; } = "";
    public int CustomerParentAge { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
