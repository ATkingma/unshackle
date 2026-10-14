using AutoMapper;

namespace Cfo.Cats.Application.Common.Models;

public class Note
{
    public string Message { get; set; } = "";
}

public static class Notes
{
    public class NoteDto
    {
        public string Message { get; set; } = "";

        private class Mapping : Profile
        {
            public Mapping() =>
                CreateMap<Note, NoteDto>(MemberList.None)
                    .ReverseMap();
        }
    }
}
