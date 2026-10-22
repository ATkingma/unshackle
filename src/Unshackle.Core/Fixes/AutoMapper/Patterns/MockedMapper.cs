using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// One mocked IMapper a test file holds in a variable (TAAK-311, council A1): the classes the file constructs with it, the Map calls it stubs,
/// and why its stubs cannot simply go (null when every use of the mock is a stub or a constructor argument).
/// </summary>
internal class MockedMapper
{
    /// <summary>The local, field or property that holds the mock.</summary>
    public required ISymbol Mock { get; init; }

    /// <summary>The file, the member or type that declares the mock, and its name: the same across semantic models of one file.</summary>
    public required string Key { get; init; }

    /// <summary>The classes of the solution the file constructs with this mock as an IMapper argument.</summary>
    public required IReadOnlyList<INamedTypeSymbol> Classes { get; init; }

    /// <summary>The statements that stub a Map call of the mock with a fixed value.</summary>
    public required IReadOnlyList<MapStub> Stubs { get; init; }

    /// <summary>Why the mock serves more than stubs and constructor arguments (with file:line), or null.</summary>
    public required string? Problem { get; init; }

    /// <summary>File:line of the mock's declaration.</summary>
    public required string Where { get; init; }
}
