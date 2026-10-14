using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace ToDo;

public class Share
{
    public int UserId { get; set; }
    public bool IsArchived { get; set; }
}

public class ToDoList
{
    public bool IsArchived { get; set; }
    public List<Share> Shares { get; set; } = [];
}

public class ListDto
{
    public bool IsArchived { get; set; }
}

public class IsArchivedResolver : IValueResolver<ToDoList, object, bool>
{
    public bool Resolve(ToDoList source, object destination, bool destinationMember, ResolutionContext context)
    {
        var userId = (int)context.Items["UserId"];

        var share = source.Shares.FirstOrDefault(x => x.UserId == userId);
        if (share is not null)
        {
            return share.IsArchived;
        }

        return source.IsArchived;
    }
}

public class ToDoProfile : Profile
{
    public ToDoProfile()
    {
        CreateMap<ToDoList, ListDto>()
            .ForMember(x => x.IsArchived, opt => opt.MapFrom<IsArchivedResolver>());
    }
}
