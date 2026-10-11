using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<string, int> x)
    {
        System.Action a = () => x.Should().ContainKey("a");
    }
}
