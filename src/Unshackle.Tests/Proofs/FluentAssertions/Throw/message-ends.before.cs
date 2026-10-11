using System;
using FluentAssertions;

namespace Proofs;

public class MessageEndsCase
{
    public void Test(Action act)
    {
        act.Should().Throw<InvalidOperationException>().WithMessage("*shipped");
    }
}
