namespace Unshackle.Core.Models;

// One MSBuild property of the project file set to a value (added to an unconditional PropertyGroup when missing). A property that is
// set more than once or in a conditional group is Manual. OutputType and IsTestProject always warn (council round 8, S3).
public class PropertyEdit : ProjectFileEdit
{
    public required string Name { get; init; }

    public required string Value { get; init; }

    public override string Describe() => $"<{Name}> set to {Value}";
}
