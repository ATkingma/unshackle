using System;
using AutoMapper;

namespace Social;

public class ProfileId
{
    public ProfileId(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; }
}

public class Post
{
    public ProfileId Author { get; set; } = new(Guid.Empty);
    public string Published { get; set; } = "";
}

public class PostDto
{
    public Guid Author { get; set; }
    public DateTime Published { get; set; }
}

public class SocialProfile : Profile
{
    public SocialProfile()
    {
        CreateMap<Guid, ProfileId>(MemberList.None)
            .ConvertUsing(id => new(id));
        CreateMap<ProfileId, Guid>(MemberList.None)
            .ConvertUsing(id => id.Id);
        CreateMap<string, DateTime>().ConvertUsing((s, _) => DateTime.ParseExact(s, "yyyyMMdd", null));
        CreateMap<Post, PostDto>();
    }
}
