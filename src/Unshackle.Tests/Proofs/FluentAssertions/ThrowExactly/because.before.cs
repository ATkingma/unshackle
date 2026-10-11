using System;
using FluentAssertions;

namespace Proofs;

public class BecauseCase
{
    public void Test(Action act)
    {
        act.Should().ThrowExactly<InvalidOperationException>("it is shipped");
    }
}
