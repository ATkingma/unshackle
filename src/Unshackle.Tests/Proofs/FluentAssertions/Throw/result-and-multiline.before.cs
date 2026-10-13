using System;
using FluentAssertions;

namespace Proofs;

public class ResultAndMultilineCase
{
    public void Test(Action act)
    {
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("maxParallelization");
    }
}
