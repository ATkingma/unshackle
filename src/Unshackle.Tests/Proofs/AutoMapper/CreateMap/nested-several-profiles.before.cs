using AutoMapper;

namespace Cfo.Cats.Application.Features.PRIs.Commands;

public class PriDto
{
    public string Region { get; set; } = "";
    public string Notes { get; set; } = "";
}

public static class AddPri
{
    public class PriReleaseDto
    {
        public string Region { get; set; } = "";

        private class Mapper : Profile
        {
            public Mapper() =>
                CreateMap<PriDto, PriReleaseDto>(MemberList.None);
        }
    }

    public class PriMeetingDto
    {
        public string Notes { get; set; } = "";

        private class Mapper : Profile
        {
            public Mapper() =>
                CreateMap<PriDto, PriMeetingDto>(MemberList.None);
        }
    }
}
