using AutoMapper;

namespace Shop;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}

public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class Tag
{
    public string Value { get; set; } = "";
}

public class TagDto
{
    public string Value { get; set; } = "";
}

public class Note
{
    public string Text { get; set; } = "";
}

public class NoteDto
{
    public string Text { get; set; } = "";
}

public class ShopProfile : Profile
{
    public ShopProfile()
    {
    }
}

public static class ShopProfileExtensions
{
    public static CustomerDto ToCustomerDto(this Customer source) => new()
    {
        Id = source.Id,
        Name = source.Name,
    };

    public static Customer ToCustomer(this CustomerDto source) => new()
    {
        Id = source.Id,
        Name = source.Name,
    };
}
