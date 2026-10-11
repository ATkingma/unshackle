using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(int[] x)
    {
        x.Should().OnlyContain(i => i > 0);
    }
}
