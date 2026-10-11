using System;
using FluentAssertions;

namespace Proofs;

public class BecauseArgsCase
{
    public void Test(Action act)
    {
        act.Should().ThrowExactly<InvalidOperationException>("a {0}", 1);
    }
}
