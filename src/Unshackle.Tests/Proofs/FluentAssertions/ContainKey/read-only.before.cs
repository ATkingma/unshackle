using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(IReadOnlyDictionary<int, string> x)
    {
        x.Should().ContainKey(1);
    }
}
