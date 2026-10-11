using System;
using FluentAssertions;

namespace Proofs;

public class ValueUsedCase
{
    public void Test(Action act)
    {
        var exception = act.Should().Throw<InvalidOperationException>().Which;
    }
}
