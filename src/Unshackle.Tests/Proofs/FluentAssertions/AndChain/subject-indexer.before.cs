using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> items, string name, object obj, List<int>[] lists)
    {
        lists[0].Should().NotBeEmpty().And.HaveCount(2);
    }
}
