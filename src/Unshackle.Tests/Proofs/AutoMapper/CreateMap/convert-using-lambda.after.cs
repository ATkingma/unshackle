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
    }
}

public static class SocialProfileExtensions
{
    public static ProfileId ToProfileId(this Guid source) => new(source);

    public static Guid ToGuid(this ProfileId source) => source.Id;

    public static DateTime ToDateTime(this string source) => DateTime.ParseExact(source, "yyyyMMdd", null);

    public static PostDto ToPostDto(this Post source) => new()
    {
        Author = source.Author.ToGuid(),
        Published = source.Published.ToDateTime(),
    };
}
