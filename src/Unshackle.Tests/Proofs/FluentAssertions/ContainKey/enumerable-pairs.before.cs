using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(IEnumerable<KeyValuePair<string, int>> x)
    {
        x.Should().ContainKey("a");
    }
}
