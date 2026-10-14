using System.Collections.Generic;
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
            CreateMap<TodoList, TodoListDto>();
        }
    }
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
            CreateMap<TodoItem, TodoItemDto>().ForMember(d => d.Priority,
                opt => opt.MapFrom(s => s.Priority));
        }
    }
}

public class GetTodosQueryHandler
{
    private readonly IMapper _mapper;

    public GetTodosQueryHandler(IMapper mapper)
    {
        _mapper = mapper;
    }

    public TodoListDto Handle(TodoList list) => _mapper.Map<TodoListDto>(list);
}
