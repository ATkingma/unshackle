using System;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(int x)
    {
        x.ShouldBe(1);
    }
}
