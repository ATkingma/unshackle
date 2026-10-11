using Unshackle.Core.Models;

namespace Unshackle.Core.Abstractions;

/// <summary>A migration for one library (Strategy), for example FluentAssertions to Shouldly.</summary>
public interface IFix
{
    /// <summary>Identifier used on the command line, for example <c>fluentassertions</c>.</summary>
    string Id { get; }

    /// <summary>The patterns of this fix, run in this order on every document.</summary>
    IReadOnlyList<IPattern> Patterns { get; }

    /// <summary>
    /// Namespaces whose top-level <c>using</c> directive is removed from a changed document once nothing in that
    /// document binds to them anymore. Global and alias usings are left alone.
    /// </summary>
    IReadOnlyList<string> RemovedNamespaces { get; }

    /// <summary>Namespaces that every changed document gets a top-level <c>using</c> directive for.</summary>
    IReadOnlyList<string> AddedNamespaces { get; }

    /// <summary>
    /// The package swap in the project files, or <see langword="null"/> (the default) when the fix leaves project files alone.
    /// <c>&lt;Using Include&gt;</c> items follow <see cref="RemovedNamespaces"/> and <see cref="AddedNamespaces"/>.
    /// </summary>
    PackageMigration? Package => null;

    /// <summary>
    /// Types of <see cref="RemovedNamespaces"/> that have a namespace-swapped twin with the same name and arity in
    /// <see cref="AddedNamespaces"/>, as <c>Namespace.MetadataName</c> (for example <c>MediatR.IRequest`1</c>). Using
    /// them, or their members, does not keep the removed using or package. Empty (the default) when no type is swapped.
    /// </summary>
    IReadOnlyList<string> SwappedTypes => [];

    /// <summary>
    /// Extension methods of <see cref="RemovedNamespaces"/> that the patterns start from, called as <c>x.Name()</c> (for example
    /// <c>Should</c>). One that binds to nothing after the fix was seen by no pattern: its file keeps the removed using and package,
    /// and gets a Manual finding. Empty (the default) when the fix has none.
    /// </summary>
    IReadOnlyList<string> RemovedExtensionMethods => [];

    /// <summary>
    /// True when a partial migration is unsafe (for example code rewritten while its registration stays): if any finding is Manual,
    /// the solution and project files are left as they are and every Fixed finding is reported as Manual. False (the default): fix what can be fixed.
    /// </summary>
    bool AllOrNothing => false;
}
