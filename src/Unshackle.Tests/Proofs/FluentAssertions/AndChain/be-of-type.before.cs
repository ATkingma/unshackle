using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> items, string name, object obj)
    {
        obj.Should().NotBeNull().And.BeOfType<string>();
    }
}
