using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<string, int> x, string key)
    {
        x.Should().ContainKey(key).WhoseValue.Should().Be(1);
    }
}
