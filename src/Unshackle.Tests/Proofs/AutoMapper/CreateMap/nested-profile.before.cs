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
            CreateMap<TodoList, LookupDto>();
            CreateMap<TodoItem, LookupDto>();
        }
    }
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
            CreateMap<TodoItem, TodoItemBriefDto>();
        }
    }
}
