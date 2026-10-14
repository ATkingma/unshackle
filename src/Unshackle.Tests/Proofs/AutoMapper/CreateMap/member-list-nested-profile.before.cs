using AutoMapper;

namespace Shop;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}

public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class Tag
{
    public string Value { get; set; } = "";
}

public class TagDto
{
    public string Value { get; set; } = "";
}

public class Note
{
    public string Text { get; set; } = "";
}

public class NoteDto
{
    public string Text { get; set; } = "";

    private class Mapping : Profile
    {
        public Mapping() => CreateMap<Note, NoteDto>(MemberList.None);
    }
}
