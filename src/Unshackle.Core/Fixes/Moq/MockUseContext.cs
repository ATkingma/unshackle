using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.Moq;

// What the uses of one mock say about its defaults: members set up for every argument (no pin) and properties that remember a set
// value as in NSubstitute (SetupProperty, SetupAllProperties: no setter difference).
public class MockUseContext
{
    public List<ISymbol> SetUp { get; } = [];

    public List<ISymbol> Tracked { get; } = [];

    public bool AllTracked { get; set; }

    // Positions of the SetupProperty/SetupAllProperties statements and of the first other use besides .Object.
    public List<int> TrackingPositions { get; } = [];

    public int FirstConfiguration { get; set; } = int.MaxValue;
}
