namespace Unshackle.Core.Models;

// Exact text replacements in a known config file next to the project file. Each old text must occur exactly once, otherwise the whole
// edit is Manual. Nothing to do when the file is not there.
public class ConfigFileTransformEdit : ProjectFileEdit
{
    // Relative to the project folder.
    public required string FileName { get; init; }

    public required IReadOnlyList<TextReplacement> Replacements { get; init; }

    public override string Describe() => $"{FileName}: {Replacements.Count} exact replacement(s)";
}
