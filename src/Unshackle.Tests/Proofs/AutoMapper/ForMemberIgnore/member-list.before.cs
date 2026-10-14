using AutoMapper;

namespace Shop;

public class Tenant
{
    public string Name { get; set; } = "";
}

public class Document
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public Tenant Tenant { get; set; } = new();
}

public class DocumentDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string TenantName { get; set; } = "";
}

public class DocumentProfile : Profile
{
    public DocumentProfile()
    {
        CreateMap<Document, DocumentDto>(MemberList.None)
            .ForMember(x => x.TenantName, s => s.Ignore());
        CreateMap<DocumentDto, Document>(MemberList.None)
            .ForMember(x => x.Tenant, s => s.Ignore());
    }
}
