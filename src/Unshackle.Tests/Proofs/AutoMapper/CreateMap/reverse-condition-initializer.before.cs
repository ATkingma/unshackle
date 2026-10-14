using AutoMapper;

namespace Shop;

public class Person
{
    public int Id { get; set; }
    public string Nickname { get; set; } = "none";
}

public class PersonDto
{
    public int Id { get; set; }
    public string Nickname { get; set; } = "";
    public bool Active { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Person, PersonDto>()
            .ReverseMap()
            .ForMember(person => person.Nickname, opt => opt.Condition(dto => dto.Active));
    }
}
