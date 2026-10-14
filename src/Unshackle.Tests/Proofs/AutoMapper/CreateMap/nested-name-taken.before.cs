using AutoMapper;

namespace CleanArchitecture.Application.Common.Models;

public class TodoItem
{
    public int Id { get; set; }
}

public class LookupDto
{
    public int Id { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<TodoItem, LookupDto>();
        }
    }
}

public static class LookupDtoMappingExtensions
{
    public static string Describe(this LookupDto dto) => "Lookup " + dto.Id;
}
