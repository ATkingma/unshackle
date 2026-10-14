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
}

public class ShopProfile : Profile
{
    public ShopProfile()
    {
        CreateMap<Customer, CustomerDto>(memberList: MemberList.Destination);
        CreateMap<Tag, TagDto>(AutoMapper.MemberList.Source);
        CreateMap<Note, NoteDto>(memberList: MemberList.None);
    }
}
