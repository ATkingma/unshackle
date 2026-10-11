using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Proofs;

public class Case
{
    public void Test(List<int> items, string name, object obj)
    {
        using (new AssertionScope())
        {
            items.Should().NotBeEmpty().And.HaveCount(2);
        }
    }
}
