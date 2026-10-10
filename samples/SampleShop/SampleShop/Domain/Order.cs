namespace SampleShop.Domain;

public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public OrderStatus Status { get; set; } = OrderStatus.Placed;
    public List<OrderLine> Lines { get; set; } = [];

    public decimal Total => Lines.Sum(l => l.Quantity * l.UnitPrice);

    public void Cancel()
    {
        if (Status == OrderStatus.Shipped)
        {
            throw new InvalidOperationException("A shipped order cannot be cancelled.");
        }

        Status = OrderStatus.Cancelled;
    }
}
