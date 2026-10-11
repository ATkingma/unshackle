using System;
using FluentAssertions;

namespace Proofs;

public class BecauseArgsCase
{
    public void Test(Action act)
    {
        act.Should().Throw<InvalidOperationException>("because {0}", 1);
    }
}
