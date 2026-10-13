using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<string, int> x, List<KeyValuePair<string, int>> y)
    {
        x.Should().Equal(y);
    }
}
