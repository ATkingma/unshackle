using System;
using FluentAssertions;

namespace Proofs;

public class LambdaCase
{
    public void Test(Action act)
    {
        Action run = () => act.Should().Throw<InvalidOperationException>();
    }
}
