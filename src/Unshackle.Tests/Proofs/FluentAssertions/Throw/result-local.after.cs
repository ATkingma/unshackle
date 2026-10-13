using System;
using FluentAssertions;
using Shouldly;

namespace Proofs;

public class ResultLocalCase
{
    public void Test(Action act)
    {
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.Should().Be("id");
        ex.Message.Should().Contain("id");
    }
}
