using System;
using FluentAssertions;

namespace Proofs;

public class SimpleCase
{
    public void Test(Action act)
    {
        act.Should().Throw<InvalidOperationException>();
    }
}
