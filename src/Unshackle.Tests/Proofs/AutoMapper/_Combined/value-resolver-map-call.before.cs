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
            CreateMap<ToDoTask, TaskDto>()
                .ForMember(x => x.IsPrivate, opt => opt.MapFrom<IsPrivateResolver>())
                .ForMember(x => x.TagCount, opt => opt.MapFrom((src, dest) => src.Tags.Count()));
        }
    }

    public class TaskService
    {
        private readonly IMapper _mapper;

        public TaskService(IMapper mapper)
        {
            _mapper = mapper;
        }

        public TaskDto Get(ToDoTask task) => _mapper.Map<TaskDto>(task);
    }
}
