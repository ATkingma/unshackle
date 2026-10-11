namespace SampleShop.Domain;

public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public Address Address { get; set; } = new();
    public List<string> Tags { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
}

public record Money(decimal Amount, string Currency);

// Records in collections: used by the BeEquivalentTo/ContainEquivalentOf cases.
public record Product(string Sku, string Name, Money Price);
