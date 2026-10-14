using AutoMapper;

namespace Cfo.Cats.Application.Features.Participants.Commands;

public class Note
{
    public string ParticipantId { get; set; } = "";
    public string Message { get; set; } = "";
}

public static class AddNote
{
    public class Command
    {
        public string ParticipantId { get; set; } = "";
        public string Message { get; set; } = "";

        private class Mapping : Profile
        {
            public Mapping() => CreateMap<Command, Note>(MemberList.None);
        }
    }

    public class Handler
    {
        public string Handle(Command request) => request.Message;
    }
}
