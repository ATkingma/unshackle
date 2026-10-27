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
        CreateMap<T, T>();
    }
}

internal class RoleProfile : CopyProfile<Role>
{
}

internal class TenantProfile : CopyProfile<Tenant>
{
}
