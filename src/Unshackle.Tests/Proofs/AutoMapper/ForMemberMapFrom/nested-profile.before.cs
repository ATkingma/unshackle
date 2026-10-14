using AutoMapper;

namespace CleanArchitecture.Application.TodoLists.Queries.GetTodos;

public enum PriorityLevel
{
    None,
    Low,
    High,
}

public class TodoItem
{
    public int Id { get; set; }
    public int ListId { get; set; }
    public string Title { get; set; } = "";
    public bool Done { get; set; }
    public PriorityLevel Priority { get; set; }
    public string Note { get; set; } = "";
}

public class TodoItemDto
{
    public int Id { get; init; }

    public int ListId { get; init; }

    public string Title { get; init; } = "";

    public bool Done { get; init; }

    public int Priority { get; init; }

    public string Note { get; init; } = "";

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<TodoItem, TodoItemDto>().ForMember(d => d.Priority, 
                opt => opt.MapFrom(s => (int)s.Priority));
        }
    }
}
