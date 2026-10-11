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
}
