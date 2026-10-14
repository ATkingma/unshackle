using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace ToDo;

public class Share
{
    public bool IsAccepted { get; set; }
}

public class ToDoList
{
    public int Id { get; set; }
    public List<Share> Shares { get; set; } = [];
}

public class ListDto
{
    public int Id { get; set; }
    public bool IsShared { get; set; }
    public int ShareCount { get; set; }
}

public class IsSharedResolver : IValueResolver<ToDoList, object, bool>
{
    public bool Resolve(ToDoList source, object destination, bool destinationMember, ResolutionContext context)
    {
        return source.Shares.Any(x => x.IsAccepted);
    }
}

public class ShareCountResolver : IValueResolver<ToDoList, ListDto, int>
{
    public int Resolve(ToDoList source, ListDto destination, int destMember, ResolutionContext context) => source.Shares.Count;
}

public class ToDoProfile : Profile
{
    public ToDoProfile()
    {
        CreateMap<ToDoList, ListDto>()
            .ForMember(x => x.IsShared, opt => opt.MapFrom<IsSharedResolver>())
            .ForMember(x => x.ShareCount, opt => opt.MapFrom(new ShareCountResolver()));
    }
}
