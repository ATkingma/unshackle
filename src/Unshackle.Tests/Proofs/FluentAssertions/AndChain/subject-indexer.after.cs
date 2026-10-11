using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> items, string name, object obj, List<int>[] lists)
    {
        var subject = lists[0];
        subject.ShouldNotBeEmpty();
        subject.Count.ShouldBe(2);
    }
}
