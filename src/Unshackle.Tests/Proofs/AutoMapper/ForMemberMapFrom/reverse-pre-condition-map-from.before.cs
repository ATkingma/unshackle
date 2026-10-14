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
        CreateMap<Person, PersonDto>()
            .ForMember(dto => dto.Alias, opt => opt.MapFrom(person => person.Nickname))
            .ReverseMap()
            .ForMember(person => person.Nickname, opt => opt.PreCondition(dto => dto.Active));
    }
}
