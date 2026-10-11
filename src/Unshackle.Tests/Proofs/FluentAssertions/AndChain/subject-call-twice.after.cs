using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> items, string name, object obj)
    {
        var subject = GetItems();
        subject.ShouldNotBeEmpty();
        subject.Count.ShouldBe(2);
        var subject2 = GetItems();
        subject2.ShouldContain(1);
        subject2.Count.ShouldBe(2);
    }

    private static List<int> GetItems() => [1, 2];
}
