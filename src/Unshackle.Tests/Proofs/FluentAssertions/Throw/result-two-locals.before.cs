using System;
using FluentAssertions;

namespace Proofs;

public class ResultTwoLocalsCase
{
    public void Test(Action act)
    {
        act.Should().Throw<ArgumentException>().WithMessage("*id*").And.ParamName.Should().Be("id");
        act.Should().Throw<ArgumentException>().WithMessage("*id*").Where(e => e.ParamName != null);
    }
}
