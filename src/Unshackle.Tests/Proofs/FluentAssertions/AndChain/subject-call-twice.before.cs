using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> items, string name, object obj)
    {
        GetItems().Should().NotBeEmpty().And.HaveCount(2);
        GetItems().Should().Contain(1).And.HaveCount(2);
    }

    private static List<int> GetItems() => [1, 2];
}
