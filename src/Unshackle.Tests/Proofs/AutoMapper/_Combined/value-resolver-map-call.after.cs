using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace ToDo
{
    public class ToDoTask
    {
        public int Id { get; set; }
        public int? PrivateToUserId { get; set; }
        public List<string> Tags { get; set; } = [];
    }

    public class TaskDto
    {
        public int Id { get; set; }
        public bool IsPrivate { get; set; }
        public int TagCount { get; set; }
    }

    public class IsPrivateResolver : IValueResolver<ToDoTask, TaskDto, bool>
    {
        public bool Resolve(ToDoTask source, TaskDto destination, bool destMember, ResolutionContext context) => source.PrivateToUserId.HasValue;
    }

    public class TaskProfile : Profile
    {
        public TaskProfile()
        {
        }
    }

    public static class TaskProfileExtensions
    {
        public static TaskDto ToTaskDto(this ToDoTask source) => new()
        {
            Id = source.Id,
            IsPrivate = new IsPrivateResolver().Resolve(source, default!, default!, default!),
            TagCount = source.Tags.Count(),
        };
    }

    public class TaskService
    {

        public TaskService()
        {
        }

        public TaskDto Get(ToDoTask task) => task.ToTaskDto();
    }
}
