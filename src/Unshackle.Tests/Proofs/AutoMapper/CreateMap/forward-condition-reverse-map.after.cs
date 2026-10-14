using AutoMapper;

namespace Shop;

public class Person
{
    public int Id { get; set; }
    public string? Nickname { get; set; }
    public bool Active { get; set; }
}

public class PersonDto
{
    public int Id { get; set; }
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
        Nickname = source.Active ? source.Nickname : default,
        Active = source.Active,
    };

    public static Person ToPerson(this PersonDto source) => new()
    {
        Id = source.Id,
        Nickname = source.Nickname,
        Active = source.Active,
    };
}
