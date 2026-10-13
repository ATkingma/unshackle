using System;
using FluentAssertions;
using Shouldly;

namespace Proofs;

public class ResultTwoLocalsCase
{
    public void Test(Action act)
    {
        var exception = Should.Throw<ArgumentException>(act);
        exception.Message.ShouldContain("id", Shouldly.Case.Insensitive);
        exception.ParamName.Should().Be("id");
        var exception2 = Should.Throw<ArgumentException>(act);
        exception2.Message.ShouldContain("id", Shouldly.Case.Insensitive);
        (exception2.ParamName != null).ShouldBeTrue();
    }
}
