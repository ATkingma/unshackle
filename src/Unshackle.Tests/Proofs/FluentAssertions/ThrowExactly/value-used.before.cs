using System;
using FluentAssertions;

namespace Proofs;

public class ValueUsedCase
{
    public void Test(Action act)
    {
        string message = act.Should().ThrowExactly<InvalidOperationException>().Which.Message;
    }
}
