using System;
using AutoMapper;

namespace Shop;

public class Note
{
    public int Id { get; set; }
    public string? Content { get; set; }
}

public class NoteDto
{
    public int Id { get; set; }
    public string? Summary { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Note, NoteDto>()
            .ForMember(dest => dest.Summary, opt =>
            {
                opt.Condition(src => src.Content != null);
                opt.MapFrom(src => src.Content);
            });
    }
}
