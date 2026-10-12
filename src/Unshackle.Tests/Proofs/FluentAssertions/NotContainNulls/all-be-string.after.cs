using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(IEnumerable<string> x)
    {
        x.ShouldAllBe(i => i == "EUR");
    }
}
