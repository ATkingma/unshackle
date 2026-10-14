using System;
using AutoMapper;

namespace Shop;

public class PostForm
{
    public Guid Id { get; set; }
    public string Content { get; set; } = "";
}

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<PostForm, Post>()
            .ForMember(post => post.Id, opt => opt.Condition(form => form.Id != Guid.Empty));
    }
}
