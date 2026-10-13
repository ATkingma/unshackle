using System;
using FluentAssertions;
using Shouldly;

namespace Proofs;

public class ResultAndMultilineCase
{
    public void Test(Action act)
    {
        Should.Throw<ArgumentOutOfRangeException>(act).ParamName.Should().Be("maxParallelization");
    }
}
