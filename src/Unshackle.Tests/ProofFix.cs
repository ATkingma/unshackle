using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;

namespace Unshackle.Tests;

// Per fix folder in Proofs/: the fix and the references "before" (old library) and "after" (new library) compile against.
public class ProofFix
{
    public required IFix Fix { get; init; }

    public required IReadOnlyList<MetadataReference> Before { get; init; }

    public required IReadOnlyList<MetadataReference> After { get; init; }

    // Source generators of the new library that "after" needs to compile (Mediator writes AddMediator).
    public IReadOnlyList<ISourceGenerator> AfterGenerators { get; init; } = [];

    // An after proof runs every pattern of the fix, not only the folder's (TAAK-340): in a rename fix a pattern's rewrite (CombinedTags)
    // only compiles together with the namespace swap. The folder's pattern must still report a finding. Manual proofs stay single-pattern.
    public bool AfterRunsWholeFix { get; init; }
}
