using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<string> x, string currency)
    {
        x.ShouldAllBe(i => i == currency);
    }
}
