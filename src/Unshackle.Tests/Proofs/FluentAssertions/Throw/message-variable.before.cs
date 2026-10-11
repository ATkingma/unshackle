using System;
using FluentAssertions;

namespace Proofs;

public class MessageVariableCase
{
    public void Test(Action act, string pattern)
    {
        act.Should().Throw<InvalidOperationException>().WithMessage(pattern);
    }
}
