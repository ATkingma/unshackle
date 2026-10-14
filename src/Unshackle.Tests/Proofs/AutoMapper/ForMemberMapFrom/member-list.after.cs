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
    }
}

public static class DocumentProfileExtensions
{
    public static DocumentDto ToDocumentDto(this Document source) => new()
    {
        Id = source.Id,
        Title = source.Title,
        TenantName = source.Title + " (" + source.Id + ")",
    };

    public static Document ToDocument(this DocumentDto source) => new()
    {
        Id = source.Id,
        Title = source.Title,
    };
}
