using System;
using FluentAssertions;

namespace Proofs;

public class ResultParamNameCase
{
    public void Test(Action act)
    {
        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }
}
