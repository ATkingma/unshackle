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
    }
}

public static class AutoMapperProfileExtensions
{
    public static RegionDto ToRegionDto(this Region source) => new()
    {
        Id = source.Id,
        RegionName = source.RegionName,
    };

    public static Region ToRegion(this RegionDto source) => new()
    {
        Id = source.Id,
        RegionName = source.RegionName,
    };

    public static FolderEntity ToFolderEntity(this Folder source) => new()
    {
        Id = source.Id,
        UserId = source.UserId,
        Name = source.Name,
    };

    public static Folder ToFolder(this FolderEntity source) => new()
    {
        Id = source.Id,
        UserId = source.UserId,
        Name = source.Name,
    };
}
