using System;
using AutoMapper;

namespace Shop;

internal class Role
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

internal static class Registrations
{
    public static Type Open => typeof(CopyProfile<>);
}
