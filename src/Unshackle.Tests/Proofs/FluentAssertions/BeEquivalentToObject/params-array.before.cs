using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(IEnumerable<string> names, string[] expected)
    {
        names.Should().BeEquivalentTo(expected);
        names.Should().BeEquivalentTo(new[] { "A", "B" });
    }
}
