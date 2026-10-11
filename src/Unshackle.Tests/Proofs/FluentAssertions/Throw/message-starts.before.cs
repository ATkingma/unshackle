using System;
using FluentAssertions;

namespace Proofs;

public class MessageStartsCase
{
    public void Test(Action act)
    {
        act.Should().Throw<InvalidOperationException>().WithMessage("Order*");
    }
}
