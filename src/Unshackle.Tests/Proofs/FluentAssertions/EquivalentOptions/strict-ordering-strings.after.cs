using Shouldly;
using System;
using System.Collections.Generic;

namespace Proofs;

public class Case
{
    public void Test(List<string> a, string[] b)
    {
        a.ShouldBe(b);
    }
}
