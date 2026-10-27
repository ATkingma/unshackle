using AutoMapper;

namespace Shop;

public class Role
{
    public int Id { get; set; }
}

public class CopyProfile<T> : Profile
{
    public CopyProfile()
    {
        CreateMap<T, T>();
    }
}

public class RoleProfile : CopyProfile<Role>
{
}
