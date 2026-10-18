#nullable enable
using System;
using System.Linq;
using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace Shop;

public class Note
{
    public string Title { get; set; } = "";
}

public class NoteEntity
{
    public string Title { get; set; } = "";

    public required string UserId { get; set; }
}

public class NoteProfile : Profile
{
    public NoteProfile()
    {
    }
}

public static class NoteProfileExtensions
{
    public static NoteEntity ToNoteEntity(this Note source) => new()
    {
        Title = source.Title,
        UserId = default!,
    };

    public static Expression<Func<Note, NoteEntity>> NoteToNoteEntityProjection => source => new NoteEntity
    {
        Title = source.Title,
        UserId = default!,
    };
}

public class NoteQueries
{
    public IQueryable<NoteEntity> All(IQueryable<Note> notes, IMapper mapper) =>
        notes.Select(NoteProfileExtensions.NoteToNoteEntityProjection);
}
