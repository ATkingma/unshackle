using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;

namespace Unshackle.Tests;

// Per fix folder in Proofs/: the fix and the references "before" (old library) and "after" (new library) compile against.
public class ProofFix
{
    public required IFix Fix { get; init; }

    public required IReadOnlyList<MetadataReference> Before { get; init; }

    public required IReadOnlyList<MetadataReference> After { get; init; }
}
