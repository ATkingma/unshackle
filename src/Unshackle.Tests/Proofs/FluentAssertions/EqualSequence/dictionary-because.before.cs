using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(IImmutableDictionary<string, int?> x, Dictionary<string, int?> y)
    {
        x.Should().Equal(y, "the {0} counts", "node");
    }
}
