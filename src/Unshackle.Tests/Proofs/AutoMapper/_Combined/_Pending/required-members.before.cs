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
        CreateMap<NoteDto, NoteEntity>();
    }
}
