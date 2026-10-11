using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> items, string name, object obj)
    {
        obj.ShouldNotBeNull();
        obj.ShouldBeOfType<string>();
    }
}
