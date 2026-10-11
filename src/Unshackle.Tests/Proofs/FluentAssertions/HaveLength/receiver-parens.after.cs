using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(string a, string b)
    {
        (a + b).Length.ShouldBe(5);
    }
}
