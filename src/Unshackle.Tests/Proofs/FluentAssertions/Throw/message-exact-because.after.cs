using System;
using Shouldly;

namespace Proofs;

public class MessageExactBecauseCase
{
    public void Test(Action act)
    {
        Should.Throw<InvalidOperationException>(act).Message.ShouldBe("Order shipped", "b", Shouldly.StringCompareShould.IgnoreCase);
    }
}
