using System;
using Shouldly;

namespace Proofs;

public class MessageExactCase
{
    public void Test(Action act)
    {
        Should.Throw<InvalidOperationException>(act).Message.ShouldBe("Order shipped", Shouldly.StringCompareShould.IgnoreCase);
    }
}
