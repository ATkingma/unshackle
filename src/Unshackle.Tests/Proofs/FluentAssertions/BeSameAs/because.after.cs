using Shouldly;
using System;

namespace Proofs;

public class Case
{
    public void Test(string a, string b)
    {
        a.ShouldBeSameAs(b, "cached");
    }
}
