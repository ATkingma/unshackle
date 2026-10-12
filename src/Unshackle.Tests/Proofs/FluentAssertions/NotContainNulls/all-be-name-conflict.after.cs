using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<string> x, string i)
    {
        x.ShouldAllBe(item => item == i);
    }
}
