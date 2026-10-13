using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<string> lines)
    {
        lines.Should().BeEquivalentTo(
            "first",
            // a blank line
            "",
            "last");
    }
}
