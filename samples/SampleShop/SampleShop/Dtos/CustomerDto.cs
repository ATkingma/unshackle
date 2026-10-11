namespace SampleShop.Dtos;

public class CustomerDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public AddressDto Address { get; set; } = new();
    public string TagList { get; set; } = "";
    public string Since { get; set; } = "";
    public bool IsMapped { get; set; }
}

public class AddressDto
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
}

public class ProductDto
{
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string Price { get; set; } = "";
}
