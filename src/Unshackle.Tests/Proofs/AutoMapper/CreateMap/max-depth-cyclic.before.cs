using AutoMapper;

namespace Shop;

public class Comment
{
    public int Id { get; set; }
    public Comment? Reply { get; set; }
}

public class CommentDto
{
    public int Id { get; set; }
    public CommentDto? Reply { get; set; }
}

public class CommentProfile : Profile
{
    public CommentProfile()
    {
        CreateMap<Comment, CommentDto>().MaxDepth(64);
    }
}
