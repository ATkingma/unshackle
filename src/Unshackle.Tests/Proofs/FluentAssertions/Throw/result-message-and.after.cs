using System;
using FluentAssertions;
using Shouldly;

namespace Proofs;

public class ResultMessageAndCase
{
    public void Test(Action act)
    {
        var exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldStartWith("Order", Shouldly.Case.Insensitive);
        exception.HelpLink.Should().Be("help");
    }
}
