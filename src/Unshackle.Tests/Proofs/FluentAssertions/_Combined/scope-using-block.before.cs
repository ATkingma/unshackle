using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Proofs;

public class Case
{
    public void Test(int x, List<int> items, Action act)
    {
        using (new AssertionScope())
        {
            x.Should().Be(1);
            items.Should().NotBeEmpty().And.HaveCount(2);
            act.Should().Throw<InvalidOperationException>();
        }
    }
}
