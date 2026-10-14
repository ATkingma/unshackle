using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace Letters;

public class Content
{
    public string ContentType { get; set; } = "";
}

public class Post
{
    public Uri? Replies { get; set; }
    public List<Content> Contents { get; set; } = [];
}

public class NoteObject
{
    public string? MediaType { get; set; }
    public string? RepliesId { get; set; }
}

public class MediaTypeResolver : IMemberValueResolver<Post, NoteObject, Content?, string?>
{
    public string? Resolve(Post source, NoteObject destination, Content? sourceMember, string? destMember, ResolutionContext context)
    {
        return sourceMember is { } content ? content.ContentType : default;
    }
}

public class RepliesResolver : IMemberValueResolver<Post, NoteObject, Uri?, string?>
{
    public string? Resolve(Post source, NoteObject destination, Uri? sourceMember, string? destMember, ResolutionContext context) => sourceMember?.ToString();
}

public class PostProfile : Profile
{
    public PostProfile()
    {
    }
}

public static class PostProfileExtensions
{
    public static NoteObject ToNoteObject(this Post source) => new()
    {
        MediaType = new MediaTypeResolver().Resolve(source, default!, source.Contents.FirstOrDefault(), default!, default!),
        RepliesId = new RepliesResolver().Resolve(source, default!, source.Replies, default!, default!),
    };
}
