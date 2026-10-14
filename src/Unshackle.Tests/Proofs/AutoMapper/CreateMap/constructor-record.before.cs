using System;
using AutoMapper;

namespace Shop;

public class AccessList
{
    public Guid Id { get; set; }
    public string Identifier { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTimeOffset Created { get; set; }
}

public record AccessListInfoDto(Guid Id, string Identifier, string Name, string Description);

public class AccessListProfile : Profile
{
    public AccessListProfile()
    {
        CreateMap<AccessList, AccessListInfoDto>();
    }
}
