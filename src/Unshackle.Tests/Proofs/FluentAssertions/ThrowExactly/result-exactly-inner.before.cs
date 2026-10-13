using System;
using FluentAssertions;

namespace Proofs;

public class ResultExactlyInnerCase
{
    public void Test(Action act)
    {
        act.Should().ThrowExactly<InvalidOperationException>().WithInnerException<ArgumentException>();
    }
}
