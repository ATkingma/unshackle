using System;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(Action act)
    {
        Should.Throw<InvalidOperationException>(act).Message.ShouldBe("boom", Shouldly.StringCompareShould.IgnoreCase);
    }
}
