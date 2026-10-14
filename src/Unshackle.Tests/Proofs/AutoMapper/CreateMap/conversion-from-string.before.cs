using AutoMapper;

namespace Shop;

public enum Status { Open, Closed }

public class OrderForm
{
    public string Quantity { get; set; } = "";
    public string Price { get; set; } = "";
    public string Paid { get; set; } = "";
    public string OrderedAt { get; set; } = "";
    public string Status { get; set; } = "";
}

public class Order
{
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public bool Paid { get; set; }
    public System.DateTime OrderedAt { get; set; }
    public Status Status { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<OrderForm, Order>();
    }
}
