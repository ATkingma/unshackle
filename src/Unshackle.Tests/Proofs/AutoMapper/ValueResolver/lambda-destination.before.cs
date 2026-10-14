using System;
using AutoMapper;

namespace Letters;

public class Post
{
    public Uri FediId { get; set; } = new("https://example.org/1");
}

public class PostDto
{
    public Uri FediId { get; set; } = new("https://example.org/0");
    public string Hostname { get; set; } = "";
}

public class PostProfile : Profile
{
    public PostProfile()
    {
        CreateMap<Post, PostDto>()
            .ForMember(dest => dest.Hostname, opt => opt.MapFrom((_, post) => post.FediId.Host));
    }
}
