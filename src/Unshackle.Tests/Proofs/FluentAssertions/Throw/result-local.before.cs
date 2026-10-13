using System;
using FluentAssertions;

namespace Proofs;

public class ResultLocalCase
{
    public void Test(Action act)
    {
        var ex = act.Should().Throw<ArgumentException>();
        ex.And.ParamName.Should().Be("id");
        ex.Which.Message.Should().Contain("id");
    }
}
