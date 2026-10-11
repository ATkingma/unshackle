using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<string> lines)
    {
        lines.Should().Equal(
            "first",
            // a blank line
            "",
            "last");
    }
}
