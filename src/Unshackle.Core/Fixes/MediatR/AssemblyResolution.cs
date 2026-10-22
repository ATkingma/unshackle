using Microsoft.CodeAnalysis;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR;

/// <summary>
/// The assemblies that the non-literal sources of an AddMediatR scan (an assembly parameter, a method type parameter) resolve to through
/// their callers in the solution (council round 8, A2), and why the others stay unknown.
/// </summary>
public class AssemblyResolution
{
    /// <summary>The reason code of a scan whose assembly is not statically known (council round 8, A2).</summary>
    public const string AssemblyNotStaticallyKnown = nameof(MediatRReason.AssemblyNotStaticallyKnown);

    /// <summary>Nothing resolved: without the solution no caller is known.</summary>
    public static AssemblyResolution None { get; } = new()
    {
        Resolved = new Dictionary<ISymbol, IAssemblySymbol>(SymbolEqualityComparer.Default),
        Unknown = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default),
    };

    /// <summary>Per parameter or type parameter: the one assembly every caller passes, as seen from the scanning project.</summary>
    public required IReadOnlyDictionary<ISymbol, IAssemblySymbol> Resolved { get; init; }

    /// <summary>Per parameter or type parameter that does not resolve: why, for the report.</summary>
    public required IReadOnlyDictionary<ISymbol, string> Unknown { get; init; }

    /// <summary>
    /// The Manual result for a source that stays unknown (council round 10, C1): code <see cref="MediatRReason.AssemblyNotStaticallyKnown"/>
    /// with why, then the text; rendered as before ("AssemblyNotStaticallyKnown: why. text"). A source the resolver did not follow (no why)
    /// keeps its plain text "AssemblyNotStaticallyKnown. text", so the report stays byte-identical.
    /// </summary>
    public PatternResult Manual(ISymbol source, string text) =>
        Unknown.TryGetValue(source, out string? why)
            ? PatternResult.Manual(MediatRReason.AssemblyNotStaticallyKnown, $"{why}. {text}")
            : PatternResult.Manual($"{AssemblyNotStaticallyKnown}. {text}");
}
