using System.Collections.Generic;
using AutoMapper;

namespace Shop;

public class Post
{
    public int Id { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class PostDto
{
    public int Id { get; set; }
    public List<string> Tags { get; } = new();
}

public class PostProfile : Profile
{
    public PostProfile()
    {
        CreateMap<Post, PostDto>();
    }
}
