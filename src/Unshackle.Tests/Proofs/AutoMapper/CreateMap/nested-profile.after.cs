using AutoMapper;

namespace CleanArchitecture.Application.Common.Models;

public class TodoList
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
}

public class TodoItem
{
    public int Id { get; set; }
    public int ListId { get; set; }
    public string Title { get; set; } = "";
    public bool Done { get; set; }
}

public class LookupDto
{
    public int Id { get; init; }

    public string Title { get; init; } = "";

    private class Mapping : Profile
    {
        public Mapping()
        {
        }
    }
}

public static class LookupDtoMappingExtensions
{
    public static LookupDto ToLookupDto(this TodoList source) => new()
    {
        Id = source.Id,
        Title = source.Title,
    };

    public static LookupDto ToLookupDto(this TodoItem source) => new()
    {
        Id = source.Id,
        Title = source.Title,
    };
}

public class TodoItemBriefDto
{
    public int Id { get; init; }

    public int ListId { get; init; }

    public string Title { get; init; } = "";

    public bool Done { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
        }
    }
}

public static class TodoItemBriefDtoMappingExtensions
{
    public static TodoItemBriefDto ToTodoItemBriefDto(this TodoItem source) => new()
    {
        Id = source.Id,
        ListId = source.ListId,
        Title = source.Title,
        Done = source.Done,
    };
}
