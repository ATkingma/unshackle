using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(List<string> names)
    {
        names.Should().OnlyContain(n =>
            n.Length > 0
            && n != "x");
    }
}
