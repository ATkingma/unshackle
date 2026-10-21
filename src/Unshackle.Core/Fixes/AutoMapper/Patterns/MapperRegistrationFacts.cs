namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

internal class MapperRegistrationFacts
{
    public List<(string Location, string? Condition)> Calls { get; } = [];

    public List<string> Containers { get; } = [];
}
