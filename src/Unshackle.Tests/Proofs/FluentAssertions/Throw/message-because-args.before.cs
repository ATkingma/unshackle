using System;
using FluentAssertions;

namespace Proofs;

public class MessageBecauseArgsCase
{
    public void Test(Action act)
    {
        act.Should().Throw<InvalidOperationException>().WithMessage("*a*", "because {0}", 1);
    }
}
