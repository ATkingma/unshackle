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
            public Mapping()
            {
            }
        }
    }

    public class Handler
    {
        public string Handle(Command request) => request.Message;
    }
}

public static class AddNoteCommandMappingExtensions
{
    public static Note ToNote(this AddNote.Command source) => new()
    {
        ParticipantId = source.ParticipantId,
        Message = source.Message,
    };
}
