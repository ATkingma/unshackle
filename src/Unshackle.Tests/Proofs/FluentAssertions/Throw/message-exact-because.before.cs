using System;
using FluentAssertions;

namespace Proofs;

public class MessageExactBecauseCase
{
    public void Test(Action act)
    {
        act.Should().Throw<InvalidOperationException>().WithMessage("Order shipped", "b");
    }
}
