using System;
using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(Action act)
    {
        act.Should().Throw<Exception>().Which.Message.Should().StartWith("x");
    }
}
