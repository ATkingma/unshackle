using System;
using FluentAssertions;

namespace Proofs;

public class MessageExactCase
{
    public void Test(Action act)
    {
        act.Should().Throw<InvalidOperationException>().WithMessage("Order shipped");
    }
}
