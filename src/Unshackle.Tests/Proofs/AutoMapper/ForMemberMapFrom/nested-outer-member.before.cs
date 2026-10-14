using AutoMapper;

namespace CleanArchitecture.Blazor.Application.Features.AuditTrails.DTOs;

public class AuditTrail
{
    public int Id { get; set; }
    public int PrimaryKey { get; set; }
}

public class AuditTrailDto
{
    public int Id { get; set; }

    public string PrimaryKey { get; set; } = "";

    public static string Serialize(int key) => "{\"Id\":" + key + "}";

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<AuditTrail, AuditTrailDto>(MemberList.None)
                .ForMember(x => x.PrimaryKey,
                    s => s.MapFrom(y => Serialize(y.PrimaryKey)));
        }
    }
}
