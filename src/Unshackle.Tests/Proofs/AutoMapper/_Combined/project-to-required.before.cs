#nullable enable
using System.Linq;
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
        CreateMap<Note, NoteEntity>();
    }
}

public class NoteQueries
{
    public IQueryable<NoteEntity> All(IQueryable<Note> notes, IMapper mapper) =>
        notes.ProjectTo<NoteEntity>(mapper.ConfigurationProvider);
}
