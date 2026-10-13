using System;
using FluentAssertions;

namespace Proofs;

public class ResultMessageAndCase
{
    public void Test(Action act)
    {
        act.Should().Throw<InvalidOperationException>().WithMessage("Order*").And.HelpLink.Should().Be("help");
    }
}
