using System;
using FluentAssertions;

namespace Proofs;

public class ResultInnerTypeCase
{
    public void Test(Action act)
    {
        act.Should().Throw<InvalidOperationException>().WithInnerException(typeof(ArgumentException));
    }
}
