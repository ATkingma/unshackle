using System;
using FluentAssertions;

namespace Proofs;

public class ThrowExactlyCase
{
    public void Test(Action act)
    {
        act.Should().ThrowExactly<InvalidOperationException>();
    }
}
