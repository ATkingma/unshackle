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
        CreateMap<Person, PersonDto>()
            .ForMember(dto => dto.Nickname, opt => opt.Condition(person => person.Active))
            .ReverseMap();
    }
}
