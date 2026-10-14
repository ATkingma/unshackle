using AutoMapper;

namespace Shop;

public class Person
{
    public int Id { get; set; }
    public string? Nickname { get; set; }
}

public class PersonDto
{
    public int Id { get; set; }
    public string? Alias { get; set; }
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
        Alias = source.Nickname,
    };

    public static Person ToPerson(this PersonDto source) => new()
    {
        Id = source.Id,
        Nickname = source.Active ? source.Alias : default,
    };
}
