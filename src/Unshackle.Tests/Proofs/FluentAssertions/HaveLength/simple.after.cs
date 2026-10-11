using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(string s)
    {
        s.Length.ShouldBe(5);
    }
}
