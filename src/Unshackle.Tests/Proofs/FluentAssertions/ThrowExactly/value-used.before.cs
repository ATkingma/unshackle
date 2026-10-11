using System;
using FluentAssertions;

namespace Proofs;

public class ValueUsedCase
{
    public void Test(Action act)
    {
        act.Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be("x");
    }
}
