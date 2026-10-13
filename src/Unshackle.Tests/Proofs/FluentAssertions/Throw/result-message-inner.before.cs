using System;
using FluentAssertions;

namespace Proofs;

public class ResultMessageInnerCase
{
    public void Test(Action act)
    {
        act.Should().Throw<InvalidOperationException>().WithMessage("Order shipped").WithInnerException<ArgumentException>();
    }
}
