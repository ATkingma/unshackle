using System;
using FluentAssertions;

namespace Proofs;

public class ResultInnerExactlyCase
{
    public void Test(Action act)
    {
        act.Should().Throw<InvalidOperationException>().WithInnerExceptionExactly<ArgumentException>("the cause is kept");
    }
}
