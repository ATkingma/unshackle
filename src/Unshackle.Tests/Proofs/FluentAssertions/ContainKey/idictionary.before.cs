using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(IDictionary<string, int> x)
    {
        x.Should().ContainKey("a");
    }
}
