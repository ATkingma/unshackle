using AutoMapper;

namespace Shop;

internal class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

internal class Tenant
{
    public int Id { get; set; }
}

internal class CopyProfile<T> : Profile
{
    public CopyProfile()
    {
    }
}

internal static class CopyProfileExtensions
{
    internal static Role ToRole(this Role source) => new()
    {
        Id = source.Id,
        Name = source.Name,
    };

    internal static Tenant ToTenant(this Tenant source) => new()
    {
        Id = source.Id,
    };
}

internal class RoleProfile : CopyProfile<Role>
{
}

internal class TenantProfile : CopyProfile<Tenant>
{
}
