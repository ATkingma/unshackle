using Microsoft.CodeAnalysis;

namespace Unshackle.Core;

// The result of one pass of the Fixer over a file: the solution with the file rewritten, whether a pattern changed it, whether the
// added namespaces were imported, and the file after the using step (null when that step did not run).
internal class FilePass
{
    public required Solution Working { get; init; }

    public required bool Changed { get; init; }

    public required bool ImportsAdded { get; init; }

    public required Document? Updated { get; init; }
}