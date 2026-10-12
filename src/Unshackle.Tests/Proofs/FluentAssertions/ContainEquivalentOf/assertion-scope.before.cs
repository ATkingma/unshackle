using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Proofs;

public class Case
{
    public void Test(List<string> x)
    {
        using (new AssertionScope())
        {
            x.Should().ContainEquivalentOf("a");
        }
    }
}
