using System;
using FluentAssertions;

namespace Proofs;

public class MessageBecauseCase
{
    public void Test(Action act)
    {
        act.Should().Throw<InvalidOperationException>("a").WithMessage("*shipped*", "b");
    }
}
