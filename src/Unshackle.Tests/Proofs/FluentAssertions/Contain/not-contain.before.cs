using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class NotContainCase
{
    public void Test(List<int> x, string text)
    {
        x.Should().NotContain(2);
        text.Should().NotContain("bad");
    }
}
