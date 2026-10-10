using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Models;

public class FixReport
{
    public required Solution Solution { get; init; }

    public required IReadOnlyList<Finding> Findings { get; init; }
}
