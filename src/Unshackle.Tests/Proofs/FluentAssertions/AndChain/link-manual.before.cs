using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> items, string name, object obj)
    {
        items.Should().NotBeEmpty().And.HaveCount(2, "of {0}", 2);
    }
}
