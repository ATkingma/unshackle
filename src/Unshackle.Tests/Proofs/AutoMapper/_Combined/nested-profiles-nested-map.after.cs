using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace CleanArchitecture.Application.TodoLists.Queries.GetTodos;

public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int Priority { get; set; }
}

public class TodoList
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public List<TodoItem> Items { get; set; } = [];
}

public class TodoListDto
{
    public TodoListDto()
    {
        Items = [];
    }

    public int Id { get; init; }

    public string Title { get; init; } = "";

    public IReadOnlyCollection<TodoItemDto> Items { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
        }
    }
}

public static class TodoListDtoMappingExtensions
{
    public static TodoListDto ToTodoListDto(this TodoList source) => new()
    {
        Id = source.Id,
        Title = source.Title,
        Items = source.Items?.Select(x => x?.ToTodoItemDto()).ToList() ?? [],
    };
}

public class TodoItemDto
{
    public int Id { get; init; }

    public string Title { get; init; } = "";

    public int Priority { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
        }
    }
}

public static class TodoItemDtoMappingExtensions
{
    public static TodoItemDto ToTodoItemDto(this TodoItem source) => new()
    {
        Id = source.Id,
        Title = source.Title,
        Priority = source.Priority,
    };
}

public class GetTodosQueryHandler
{

    public GetTodosQueryHandler()
    {
    }

    public TodoListDto Handle(TodoList list) => list.ToTodoListDto();
}
