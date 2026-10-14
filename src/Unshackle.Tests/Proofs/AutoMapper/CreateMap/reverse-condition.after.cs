using AutoMapper;

namespace Shop;

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Nickname { get; set; }
}

public class PersonDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Nickname { get; set; }
    public bool Active { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
    }
}

public static class OrderProfileExtensions
{
    public static PersonDto ToPersonDto(this Person source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Nickname = source.Nickname,
    };

    public static Person ToPerson(this PersonDto source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Nickname = source.Active ? source.Nickname : default,
    };
}
