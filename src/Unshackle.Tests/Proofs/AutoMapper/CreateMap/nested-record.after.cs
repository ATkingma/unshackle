using AutoMapper;

namespace Cfo.Cats.Application.Features.Tenants.DTOs;

public class Tenant
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

public record TenantDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";

    private class Mapping : Profile
    {
        public Mapping()
        {
        }
    }
}

public static class TenantDtoMappingExtensions
{
    public static TenantDto ToTenantDto(this Tenant source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Description = source.Description,
    };
}
