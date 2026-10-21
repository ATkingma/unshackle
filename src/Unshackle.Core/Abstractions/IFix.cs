using Microsoft.CodeAnalysis;
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
    /// Public types of <see cref="AddedNamespaces"/>, as <c>Namespace.MetadataName</c> (for example <c>Mediator.ICommand`1</c>).
    /// Where a file gets an added using, its references to an own type with the same name and arity get the full name, so they
    /// neither become ambiguous (CS0104) nor bind to the new type. Empty (the default): not checked.
    /// </summary>
    IReadOnlyList<string> AddedTypes => [];

    /// <summary>
    /// Extension methods of <see cref="RemovedNamespaces"/> that the patterns start from, called as <c>x.Name()</c> (for example
    /// <c>Should</c>). One that binds to nothing after the fix was seen by no pattern: its file keeps the removed using and package,
    /// and gets a Manual finding. Empty (the default) when the fix has none.
    /// </summary>
    IReadOnlyList<string> RemovedExtensionMethods => [];

    /// <summary>
    /// True (the default) when every pattern only finds code in a file that names the old library: a type, member or namespace of
    /// its assemblies, or a declaration whose signature names one. The Fixer then skips the patterns for a file without any such
    /// name. False runs every pattern on every file (a fix whose patterns match code of the user's own).
    /// </summary>
    bool FindsOnlyOldLibraryCode => true;

    /// <summary>
    /// True when the patterns may fix several files at the same time: every static cache and every shared mutable field of the fix
    /// and its patterns is thread-safe and gives the same answer whichever file asks first. False (the default) fixes one file at a
    /// time. The output is the same either way (council round 7).
    /// </summary>
    bool FixesFilesInParallel => false;

    /// <summary>
    /// True when a partial migration is unsafe (for example code rewritten while its registration stays): if any finding is Manual,
    /// the solution and project files are left as they are and every Fixed finding is reported as Manual. False (the default): fix what can be fixed.
    /// </summary>
    bool AllOrNothing => false;

    /// <summary>
    /// With <see cref="AllOrNothing"/>: whether this Manual finding is a planned part of a partial migration (code that safely stays on the old
    /// library) instead of a reason to leave the solution as it is. False (the default): every Manual finding undoes the fix.
    /// </summary>
    bool AllowsPartial(Finding finding) => false;

    /// <summary>
    /// Packages that build on the old library and have no version for the new one with the same API: the solution gets their
    /// source, migrated, in the project all their users reference, and loses the package (TAAK-194). Empty (the default): none.
    /// </summary>
    IReadOnlyList<VendoredPackage> VendoredPackages => [];

    /// <summary>
    /// Assemblies of the new library, added to each project's semantic model while fixing (not to the result). Without them a
    /// rewrite in a lambda binds to nothing, overload resolution of the call around it fails and patterns after it miss code
    /// that depends on that call (TAAK-116). Empty (the default): the patterns see only what the project references.
    /// </summary>
    IReadOnlyList<MetadataReference> AddedReferences => [];
}
