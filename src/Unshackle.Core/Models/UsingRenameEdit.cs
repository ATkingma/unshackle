namespace Unshackle.Core.Models;

// The project file's <Using Include="..."/> items (implicit global usings) of one namespace renamed, for example TechTalk.SpecFlow →
// Reqnroll. Nothing to do when there is none; Manual when the nearest Directory.Build.props declares one (that file is not edited).
public class UsingRenameEdit : ProjectFileEdit
{
    public required string OldNamespace { get; init; }

    public required string NewNamespace { get; init; }

    public override string Describe() => $"<Using> {OldNamespace} → {NewNamespace}";
}
