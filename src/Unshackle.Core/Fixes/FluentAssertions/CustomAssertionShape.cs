namespace Unshackle.Core.Fixes.FluentAssertions;

// What CustomAssertionAnalyzer found in a user-written FluentAssertions assertions class: either the reason it has no
// provably equivalent Shouldly form, or the extension class that replaces it.
public class CustomAssertionShape
{
    // Null when the class converts.
    public string? Reason { get; init; }

    // The assertion methods of the class (BeCancelled), for the call sites; each becomes Should{Name}.
    public IReadOnlyList<string> MethodNames { get; init; } = [];

    // The type the assertions are about (the T of ReferenceTypeAssertions<T, TSelf>), as the class declaration spells it.
    public string? SubjectType { get; init; }

    // The extension class text, first line without indentation; later lines get indentation and use eol.
    public Func<string, string, string> Render { get; init; } = (_, _) => string.Empty;

    public static CustomAssertionShape Fail(string reason) => new() { Reason = reason };
}
