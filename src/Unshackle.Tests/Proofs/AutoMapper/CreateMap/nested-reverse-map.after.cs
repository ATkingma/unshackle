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
            public Mapping()
            {
            }
        }
    }
}

public static class NotesNoteDtoMappingExtensions
{
    public static Notes.NoteDto ToNoteDto(this Note source) => new()
    {
        Message = source.Message,
    };

    public static Note ToNote(this Notes.NoteDto source) => new()
    {
        Message = source.Message,
    };
}
