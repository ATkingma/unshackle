namespace SampleShop.Dtos;

public class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public string Status { get; set; } = "";
    public int LineCount { get; set; }
    public decimal Total { get; set; }
}
