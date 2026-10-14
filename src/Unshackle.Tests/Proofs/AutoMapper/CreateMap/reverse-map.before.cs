using System;
using AutoMapper;

namespace Shop;

public class Region
{
    public long Id { get; set; }
    public string RegionName { get; set; } = "";
}

public class RegionDto
{
    public long Id { get; set; }
    public string RegionName { get; set; } = "";
}

public class Folder
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
}

public class FolderEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
}

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Region, RegionDto>().ReverseMap();
        CreateMap<Folder, FolderEntity>(MemberList.Destination).ReverseMap();
    }
}
