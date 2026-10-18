using AutoMapper;

namespace Shop;

public class BaseEntity
{
    public required int Id { get; set; }
}

public class NoteEntity : BaseEntity
{
    public string Title { get; set; } = "";

    public required string UserId { get; set; }
}

public class NoteDto
{
    public string Title { get; set; } = "";
}

public class NoteProfile : Profile
{
    public NoteProfile()
    {
    }
}

public static class NoteProfileExtensions
{
    // AutoMapper leaves an unmapped required member at default: the initializer must still name it.
    public static NoteEntity ToNoteEntity(this NoteDto source) => new()
    {
        Title = source.Title,
        Id = default,
        UserId = default!,
    };
}
