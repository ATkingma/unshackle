
namespace Proofs;

public class Order
{
    public int Id { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
}

public static class MapperHelper
{
}

public class OrderService
{
    public OrderService()
    {
    }

    public static OrderService Create() => new OrderService();
}
