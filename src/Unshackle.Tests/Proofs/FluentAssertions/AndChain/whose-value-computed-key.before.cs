using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<string, int> x)
    {
        x.Should().ContainKey(Key()).WhoseValue.Should().Be(1);
    }

    private static string Key() => "a";
}
