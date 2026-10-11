using System;
using FluentAssertions;

namespace Proofs;

public class MessageCase
{
    public void Test(Action act)
    {
        act.Should().ThrowExactly<InvalidOperationException>().WithMessage("Order*");
    }
}
