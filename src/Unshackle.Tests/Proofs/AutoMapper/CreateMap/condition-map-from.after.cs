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
    }
}

public static class OrderProfileExtensions
{
    public static NoteDto ToNoteDto(this Note source) => new()
    {
        Id = source.Id,
        Summary = source.Content != null ? source.Content : default,
    };
}
