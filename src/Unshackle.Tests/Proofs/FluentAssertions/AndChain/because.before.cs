using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> items, string name, object obj)
    {
        items.Should().NotBeEmpty("needed").And.Contain(3, "three");
    }
}
