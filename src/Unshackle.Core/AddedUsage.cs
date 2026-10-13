namespace Unshackle.Core;

// What a rewritten file uses from the added namespaces: extension calls (name and the static class that has the method)
// and types it names (e.g. Should, Case). Targeted imports are built from it (TAAK-198).
internal class AddedUsage
{
    public required IReadOnlyList<(string Name, string Type)> Calls { get; init; }

    public required IReadOnlyList<string> Types { get; init; }
}
