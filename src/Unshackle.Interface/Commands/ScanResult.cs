using Microsoft.CodeAnalysis;
using Unshackle.Core.Models;

namespace Unshackle.Interface.Commands;

public class ScanResult
{
    // The solution as loaded, before the fix.
    public required Solution Original { get; init; }

    public required FixReport Report { get; init; }
}
