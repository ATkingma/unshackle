namespace SampleShop.Dtos;

public class OrderLineDto
{
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
